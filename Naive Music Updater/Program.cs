using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CSharpFiddle
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            NaiveSongUpdate(Directory.GetCurrentDirectory(), true);
        }

        private static void NaiveSongUpdate(string folder, bool usecache)
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
                        string songname = Path.GetFileNameWithoutExtension(song);
                        Console.WriteLine("SONG:\t" + songname);
                        TagLib.File file = TagLib.File.Create(song);
                        using (file)
                        {
                            bool changed = false;
                            // don't ruin existing titles with non-file characters like slashes
                            string filetitle = string.Join("_", file.Tag.Title.Split(Path.GetInvalidFileNameChars()));
                            if (filetitle != songname)
                            {
                                file.Tag.Title = Path.GetFileNameWithoutExtension(song);
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
                                    file.Tag.Pictures = new TagLib.IPicture[0];
                                else
                                    file.Tag.Pictures = new TagLib.IPicture[] { picture };
                                changed = true;
                                Console.WriteLine($"New embedded album art");
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
