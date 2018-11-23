using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;

namespace CSharpFiddle
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
#if DEBUG
            NaiveSongUpdate(@"D:\Music");
            Console.ReadLine();
#else
            NaiveSongUpdate(Directory.GetCurrentDirectory());
#endif
        }

        // maybe make this a class or something, with settings for stuff like
        // - location of music cache
        // - "realnames" dictionary
        // - rename exceptions location
        // - whether to wipe certain properties
        // then main can load those from a file instead and pass them in
        // then just do NaiveUpdater.Start(string folder)
        private static void NaiveSongUpdate(string folder)
        {
            TagLib.Id3v2.Tag.DefaultVersion = 3;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;

            // create art folder if it's not already there
            string cache = Path.Combine(folder, ".music-cache");
            DirectoryInfo di = Directory.CreateDirectory(cache);
            di.Attributes |= FileAttributes.System | FileAttributes.Hidden;

            var pictures = new Dictionary<string, TagLib.IPicture>();
            string realnamesfile = Path.Combine(cache, "realnames.json");
            var realnames = new Dictionary<string, string>();
            if (File.Exists(realnamesfile))
                realnames = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(realnamesfile));

            // add corresponding ICOs for all PNGs
            // get art as picture form for embedding into songs
            foreach (var png in Directory.GetFiles(cache, "*.png"))
            {
                string ico = Path.ChangeExtension(png, ".ico");
                var image = Image.FromFile(png);
                using (image)
                {
                    if (!File.Exists(ico))
                        File.WriteAllBytes(Path.ChangeExtension(png, ".ico"), ConvertToIcon(image));
                    pictures.Add(Path.GetFileNameWithoutExtension(png), new TagLib.Picture(new TagLib.ByteVector((byte[])new ImageConverter().ConvertTo(image, typeof(byte[])))));
                }
            }

            string[] leavealone = File.ReadAllLines(Path.Combine(cache, "exceptions.txt"));

            foreach (var artist in Directory.GetDirectories(folder))
            {
                if (!realnames.TryGetValue(Path.GetFileName(artist), out string artistname))
                    artistname = Path.GetFileName(artist);
                if (artistname.StartsWith("."))
                    continue;
                string artisthash = artistname.GetHashCode().ToString();
                Console.WriteLine($"ARTIST:\t {artistname}");
                Console.WriteLine($"HASH:\t {artisthash}");

                // set folder icon
                string artistini = Path.Combine(artist, "desktop.ini");
                File.Delete(artistini);
                File.WriteAllText(artistini, "[.ShellClassInfo]\nIconResource = ..\\.music-cache\\" + artisthash + ".ico, 0");
                File.SetAttributes(artistini, FileAttributes.System | FileAttributes.Hidden);

                foreach (var album in Directory.GetDirectories(artist))
                {
                    if (!realnames.TryGetValue(Path.GetFileName(album), out string albumname))
                        albumname = Path.GetFileName(album);
                    string albumhash = Tuple.Create(artistname, albumname).GetHashCode().ToString();
                    Console.WriteLine($"ALBUM:\t {albumname}");
                    Console.WriteLine($"HASH:\t {albumhash}");

                    // set folder icon
                    string albumini = Path.Combine(album, "desktop.ini");
                    File.Delete(albumini);
                    File.WriteAllText(albumini, "[.ShellClassInfo]\nIconResource = ..\\..\\.music-cache\\" + albumhash + ".ico, 0");
                    File.SetAttributes(albumini, FileAttributes.System | FileAttributes.Hidden);

                    foreach (var song in Directory.GetFiles(album, "*.mp3"))
                    {
                        string location = song;
                        string currentname = Path.GetFileNameWithoutExtension(location);
                        Console.WriteLine("SONG:\t" + currentname);
                        string songname = MakeTitleCase(currentname);
                        if (leavealone.Contains(currentname))
                            songname = currentname;
                        if (currentname != songname)
                        {
                            Console.WriteLine($"Changing to title case: {songname}");
                            location = Path.Combine(Path.GetDirectoryName(location), songname + Path.GetExtension(location));
                            File.Move(song, location);
                        }
                        TagLib.File file = TagLib.File.Create(location);
                        using (file)
                        {
                            bool changed = false;
                            // don't ruin existing titles with non-file characters like slashes
                            string filetitle = file.Tag.Title == null ? songname : string.Join("_", file.Tag.Title.Split(Path.GetInvalidFileNameChars()));
                            if (filetitle != songname)
                            {
                                file.Tag.Title = songname;
                                changed = true;
                                Console.WriteLine($"New title: {file.Tag.Title}");
                            }
                            if (file.Tag.Album != albumname)
                            {
                                file.Tag.Album = albumname;
                                changed = true;
                                Console.WriteLine($"New album: {albumname}");
                            }
                            if (file.Tag.FirstAlbumArtist != artistname)
                            {
                                file.Tag.AlbumArtists = new string[] { artistname };
                                changed = true;
                                Console.WriteLine($"New album artist: {artistname}");
                            }
                            if (file.Tag.FirstComposer != artistname)
                            {
                                file.Tag.Composers = new string[] { artistname };
                                changed = true;
                                Console.WriteLine($"New composer: {artistname}");
                            }
                            if (file.Tag.FirstPerformer != artistname)
                            {
                                file.Tag.Performers = new string[] { artistname };
                                changed = true;
                                Console.WriteLine($"New performer: {artistname}");
                            }
                            pictures.TryGetValue(albumhash, out TagLib.IPicture picture);
                            if (DifferentArt(file.Tag.Pictures, picture))
                            {
                                // delete any existing pictures, or insert new picture
                                if (picture == null)
                                {
                                    Console.WriteLine("Deleted embedded album art");
                                    file.Tag.Pictures = new TagLib.IPicture[0];
                                }
                                else
                                {
                                    file.Tag.Pictures = new TagLib.IPicture[] { picture };
                                    Console.WriteLine("New embedded album art");
                                }
                                changed = true;
                            }
                            if (file.Tag.AmazonId != null)
                            {
                                Console.WriteLine($"Wiped amazon ID {file.Tag.AmazonId}");
                                file.Tag.AmazonId = null;
                                changed = true;
                            }
                            if (file.Tag.Comment != null)
                            {
                                Console.WriteLine($"Wiped comment {file.Tag.Comment}");
                                file.Tag.Comment = null;
                                changed = true;
                            }
                            if (file.Tag.Conductor != null)
                            {
                                Console.WriteLine($"Wiped conductor {file.Tag.Conductor}");
                                file.Tag.Conductor = null;
                                changed = true;
                            }
                            if (file.Tag.Copyright != null)
                            {
                                Console.WriteLine($"Wiped copyright {file.Tag.Copyright}");
                                file.Tag.Copyright = null;
                                changed = true;
                            }
                            if (file.Tag.Disc != 0)
                            {
                                Console.WriteLine($"Wiped disc number {file.Tag.Disc}");
                                file.Tag.Disc = 0;
                                changed = true;
                            }
                            if (file.Tag.DiscCount != 0)
                            {
                                Console.WriteLine($"Wiped disc count {file.Tag.DiscCount}");
                                file.Tag.DiscCount = 0;
                                changed = true;
                            }
                            if (file.Tag.FirstGenre != null)
                            {
                                Console.WriteLine($"Wiped genre {file.Tag.FirstGenre}");
                                file.Tag.Genres = new string[0];
                                changed = true;
                            }
                            if (file.Tag.Grouping != null)
                            {
                                Console.WriteLine($"Wiped grouping {file.Tag.Grouping}");
                                file.Tag.Grouping = null;
                                changed = true;
                            }
                            if (file.Tag.Lyrics != null)
                            {
                                Console.WriteLine($"Wiped lyrics {file.Tag.Lyrics}");
                                file.Tag.Lyrics = null;
                                changed = true;
                            }
                            if (file.Tag.MusicBrainzArtistId != null || file.Tag.MusicBrainzDiscId != null || file.Tag.MusicBrainzReleaseArtistId != null || file.Tag.MusicBrainzReleaseCountry != null || file.Tag.MusicBrainzReleaseId != null || file.Tag.MusicBrainzReleaseStatus != null || file.Tag.MusicBrainzReleaseType != null || file.Tag.MusicBrainzTrackId != null)
                            {
                                Console.WriteLine($"Wiped musicbrainz data");
                                file.Tag.MusicBrainzArtistId = null;
                                file.Tag.MusicBrainzDiscId = null;
                                file.Tag.MusicBrainzReleaseArtistId = null;
                                file.Tag.MusicBrainzReleaseCountry = null;
                                file.Tag.MusicBrainzReleaseId = null;
                                file.Tag.MusicBrainzReleaseStatus = null;
                                file.Tag.MusicBrainzReleaseType = null;
                                file.Tag.MusicBrainzTrackId = null;
                                changed = true;
                            }
                            if (file.Tag.MusicIpId != null)
                            {
                                Console.WriteLine($"Wiped music IP ID {file.Tag.MusicIpId}");
                                file.Tag.MusicIpId = null;
                                changed = true;
                            }
                            if (file.Tag.Track != 0)
                            {
                                Console.WriteLine($"Wiped track number {file.Tag.Track}");
                                file.Tag.Track = 0;
                                changed = true;
                            }
                            if (file.Tag.TrackCount != 0)
                            {
                                Console.WriteLine($"Wiped track count {file.Tag.TrackCount}");
                                file.Tag.TrackCount = 0;
                                changed = true;
                            }
                            if (file.Tag.Year != 0)
                            {
                                Console.WriteLine($"Wiped year {file.Tag.Year}");
                                file.Tag.Year = 0;
                                changed = true;
                            }
                            if (changed)
                            {
                                Console.WriteLine("Saving...");
                                file.Save();
                            }
                        }
                    }
                }
            }
        }

        private static string MakeTitleCase(string input)
        {
            // remove whitespace from beginning and end
            input = input.Trim();

            // turn double-spaces into single spaces
            input = Regex.Replace(input, @"\s+", " ");

            // treat parenthesized phrases like a title
            int left = input.IndexOf('(');
            int right = input.IndexOf(')');
            if (left != -1 && right != -1)
            {
                // a bit naive, but hey...
                return input.Substring(0, left) + "(" +
                    MakeTitleCase(input.Substring(left + 1, right - left - 1)) + ")" +
                    input.Substring(right + 1, input.Length - right - 1);
            }

            // treat "artist - title" style titles as two separate titles
            foreach (var separator in new string[] { "-", "–", "—", "_", "/" })
            {
                string spaced = $" {separator} ";
                string[] titles = input.Split(new[] { spaced }, StringSplitOptions.RemoveEmptyEntries);
                if (titles.Length == 1)
                    continue;
                for (int i = 0; i < titles.Length; i++)
                {
                    titles[i] = MakeTitleCase(titles[i]);
                }

                // all internal titles have already been processed, we are done
                return String.Join(spaced, titles);
            }

            string[] words = input.Split(' ');
            words[0] = Char.ToUpper(words[0][0]) + words[0].Substring(1);
            words[words.Length - 1] = Char.ToUpper(words[words.Length - 1][0]) + words[words.Length - 1].Substring(1);
            bool prevallcaps = false;
            for (int i = 1; i < words.Length - 1; i++)
            {
                bool allcaps = words[i] == words[i].ToUpper();
                if (!(allcaps && prevallcaps) && AlwaysLowercase(words[i]))
                    words[i] = Char.ToLower(words[i][0]) + words[i].Substring(1);
                else
                    words[i] = Char.ToUpper(words[i][0]) + words[i].Substring(1);
                prevallcaps = allcaps;
            }
            return String.Join(" ", words);
        }

        private static bool AlwaysLowercase(string word)
        {
            string nopunc = new String(word.Where(c => !Char.IsPunctuation(c)).ToArray());
            switch (nopunc.ToLower())
            {
                case "to":
                case "of":
                case "the":
                case "in":
                case "at":
                case "a":
                case "an":
                case "on":
                case "and":
                case "is":
                case "for":
                case "with":
                case "or":
                case "vs":
                case "from":
                case "by":
                case "as":
                case "isnt":
                case "into":

                // non-english haha
                case "de":
                case "von":
                case "la":
                case "pour":
                    return true;
                default:
                    return false;
            }
        }

        private static bool DifferentArt(TagLib.IPicture[] pictures, TagLib.IPicture picture)
        {
            if (pictures == null)
                return true;
            if (pictures.Length == 0)
                return picture != null;
            if (pictures.Length > 1)
                return true;
            if (picture == null)
                return true;
            return pictures[0].Data != picture.Data;
        }

        private static byte[] ConvertToIcon(Image image, bool preserveAspectRatio = false)
        {
            MemoryStream inputStream = new MemoryStream();
            image.Save(inputStream, ImageFormat.Png);
            inputStream.Seek(0, SeekOrigin.Begin);
            MemoryStream outputStream = new MemoryStream();
            int size = image.Size.Width;
            if (!ConvertToIcon(inputStream, outputStream, size, preserveAspectRatio))
                return null;
            return outputStream.ToArray();
        }

        private static bool ConvertToIcon(Stream input, Stream output, int size = 16, bool preserveAspectRatio = false)
        {
            var inputBitmap = (Bitmap)Bitmap.FromStream(input);
            if (inputBitmap == null)
                return false;
            float width = size, height = size;
            if (preserveAspectRatio)
            {
                if (inputBitmap.Width > inputBitmap.Height)
                    height = ((float)inputBitmap.Height / inputBitmap.Width) * size;
                else
                    width = ((float)inputBitmap.Width / inputBitmap.Height) * size;
            }
            var newBitmap = new Bitmap(inputBitmap, new Size((int)width, (int)height));
            if (newBitmap == null)
                return false;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                newBitmap.Save(memoryStream, ImageFormat.Png);
                var iconWriter = new BinaryWriter(output);
                if (output == null || iconWriter == null)
                    return false;
                iconWriter.Write((byte)0);
                iconWriter.Write((byte)0);
                iconWriter.Write((short)1);
                iconWriter.Write((short)1);
                iconWriter.Write((byte)width);
                iconWriter.Write((byte)height);
                iconWriter.Write((byte)0);
                iconWriter.Write((byte)0);
                iconWriter.Write((short)0);
                iconWriter.Write((short)32);
                iconWriter.Write((int)memoryStream.Length);
                iconWriter.Write((int)(6 + 16));
                iconWriter.Write(memoryStream.ToArray());
                iconWriter.Flush();
            }
            return true;
        }
    }
}
