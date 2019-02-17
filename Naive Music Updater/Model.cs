using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public static class ArtRetriever
    {
        private static string SourceFolder;
        private static Dictionary<string, TagLib.Picture> Gallery;
        static ArtRetriever()
        {
            Gallery = new Dictionary<string, TagLib.Picture>();
        }

        public static void SetArtSource(string folder, SearchOption search)
        {
            SourceFolder = folder;
            Gallery.Clear();
            foreach (var png in Directory.GetFiles(folder, "*.png", search))
            {
                string ico = Path.ChangeExtension(png, ".ico");
                var image = Image.FromFile(png);
                using (image)
                {
                    if (!File.Exists(ico))
                        File.WriteAllBytes(Path.ChangeExtension(png, ".ico"), ConvertToIcon(image, true));
                    Gallery.Add(Path.ChangeExtension(png.Substring(folder.Length + 1), null), new TagLib.Picture(new TagLib.ByteVector((byte[])new ImageConverter().ConvertTo(image, typeof(byte[])))));
                }
            }
        }

        public static string FullLocation(string artname)
        {
            return Path.Combine(SourceFolder, artname + ".png");
        }

        // run after scanning to declare that the current state of art has been updated in the songs
        public static void MarkAllArtRead()
        {
            foreach (var artname in Gallery.Keys)
            {
                ModifiedOptimizer.RecordChange(FullLocation(artname));
            }
        }

        public static TagLib.Picture GetArt(string name)
        {
            if (Gallery.TryGetValue(name, out TagLib.Picture result))
                return result;
            return null;
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

    // decide whether or not to skip checking files
    // by keeping track of the last time they were modified
    public static class ModifiedOptimizer
    {
        private static Dictionary<string, DateTime> LastModified;
        private static string CachePath;

        // parse cache for last modified dates
        public static void LoadCache(string filepath)
        {
            CachePath = filepath;
            LastModified = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(File.ReadAllText(filepath));
        }

        // returns true if this file has changed since the last time we recorded it
        public static bool FileDifferent(string filepath, bool result_if_no_exist)
        {
            if (!File.Exists(filepath))
                return result_if_no_exist;
            DateTime current = File.GetLastWriteTime(filepath);
            if (LastModified.TryGetValue(filepath, out DateTime cached))
                return current - TimeSpan.FromSeconds(5) > cached;
            else
                return true;
        }

        // mark this file as having been changed right now
        public static void RecordChange(string filepath)
        {
            LastModified[filepath] = DateTime.Now;
        }

        public static void SaveCache()
        {
            File.WriteAllText(CachePath, JsonConvert.SerializeObject(LastModified));
        }
    }

    public static class NameRetriever
    {
        private static List<string> SkipNames;
        private static Dictionary<string, string> NameMap;
        private static string UnderscoreReplacement = "_";
        static NameRetriever()
        {
            SkipNames = new List<string>();
            NameMap = new Dictionary<string, string>();
        }
        public static void LoadConfig(string configpath)
        {
            JObject json = JObject.Parse(File.ReadAllText(configpath));
            foreach (var skip in json["skip"])
            {
                SkipNames.Add((string)skip);
            }
            foreach (var map in (JObject)json["map"])
            {
                NameMap.Add(map.Key, (string)map.Value);
            }
            UnderscoreReplacement = (string)json["underscore"]["default"];
        }
        public static string GetName(string name, bool correctcase = false)
        {
            // 1. configurations
            foreach (var skip in SkipNames)
            {
                if (String.Equals(skip, name, StringComparison.OrdinalIgnoreCase))
                    return skip;
            }
            if (NameMap.TryGetValue(name, out string result))
                return result;
            name = name.Replace("_", UnderscoreReplacement);

            // 2. corrections
            if (correctcase)
                name = CorrectCase(name);
            return name;
        }

        public static string GetSafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        // to do: after !, the next word must be capitalized
        public static string CorrectCase(string input)
        {
            // remove whitespace from beginning and end
            input = input.Trim();

            // turn double-spaces into single spaces
            input = Regex.Replace(input, @"\s+", " ");

            // treat parenthesized phrases like a title
            int left = input.IndexOf('(');
            string spacebefore = (left > 0 && input[left - 1] == ' ') ? " " : "";
            int right = input.IndexOf(')');
            string spaceafter = (right < input.Length - 1 && input[right + 1] == ' ') ? " " : "";
            if (left != -1 && right != -1)
            {
                // a bit naive, but hey...
                return CorrectCase(input.Substring(0, left)) + spacebefore + "(" +
                    CorrectCase(input.Substring(left + 1, right - left - 1)) + ")" + spaceafter +
                    CorrectCase(input.Substring(right + 1, input.Length - right - 1));
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
                    titles[i] = CorrectCase(titles[i]);
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
                case "ad":
                case "pour":
                    return true;
                default:
                    return false;
            }
        }
    }

    public interface IHaveArt
    {
        string GetArtLocation();
    }

    public class Library
    {
        public List<Artist> Artists;
        public string Folder;
        public Library(string folder)
        {
            Folder = folder;
            Artists = new List<Artist>();
            foreach (var artist in Directory.EnumerateDirectories(folder))
            {
                if (!Path.GetFileName(artist).StartsWith("."))
                    Artists.Add(new Artist(artist));
            }
        }

        public IEnumerable<string> Save()
        {
            foreach (var artist in Artists)
            {
                foreach (var message in artist.Save())
                {
                    yield return message;
                }
            }
        }
    }

    public class Artist : IHaveArt
    {
        public List<Album> Albums;
        public string Folder;
        public string FolderName;
        public string Name;
        public TagLib.Picture Art;
        public Artist(string folder)
        {
            Folder = folder;
            FolderName = Path.GetFileName(Folder);
            Name = NameRetriever.GetName(FolderName);
            Art = ArtRetriever.GetArt(GetArtLocation());
            Albums = new List<Album>();
            foreach (var album in Directory.EnumerateDirectories(folder))
            {
                Albums.Add(new Album(album, this));
            }
        }

        public string GetArtLocation()
        {
            return FolderName;
        }

        public IEnumerable<string> Save()
        {
            yield return $"ARTIST: {Name}";
            yield return $"ART: {GetArtLocation()}";

            string artistini = Path.Combine(Folder, "desktop.ini");
            File.Delete(artistini);
            File.WriteAllText(artistini, "[.ShellClassInfo]\nIconResource = ..\\.music-cache\\art\\" + GetArtLocation() + ".ico, 0");
            File.SetAttributes(artistini, FileAttributes.System | FileAttributes.Hidden);

            foreach (var album in Albums)
            {
                foreach (var message in album.Save())
                {
                    yield return "\t" + message;
                }
            }
        }
    }

    public class Album : IHaveArt
    {
        public List<SubAlbum> SubAlbums;
        // these are only songs directly inside this album, not those inside this album's subalbums
        public List<Song> Songs;
        public string Folder;
        public string FolderName;
        public string Name;
        public Artist Parent;
        public TagLib.Picture Art;
        public Album(string folder, Artist parent)
        {
            Parent = parent;
            Folder = folder;
            FolderName = Path.GetFileName(Folder);
            Name = NameRetriever.GetName(FolderName);
            Art = ArtRetriever.GetArt(GetArtLocation());
            SubAlbums = new List<SubAlbum>();
            foreach (var album in Directory.EnumerateDirectories(folder))
            {
                SubAlbums.Add(new SubAlbum(album, this));
            }
            Songs = new List<Song>();
            foreach (var song in Directory.EnumerateFiles(folder, "*.mp3"))
            {
                Songs.Add(new Song(song, this));
            }
        }

        public string GetArtLocation()
        {
            return Path.Combine(Parent.FolderName, FolderName);
        }

        public IEnumerable<string> Save()
        {
            yield return $"ALBUM: {Name}";
            yield return $"ART: {GetArtLocation()}";

            string albumini = Path.Combine(Folder, "desktop.ini");
            File.Delete(albumini);
            File.WriteAllText(albumini, "[.ShellClassInfo]\nIconResource = ..\\..\\.music-cache\\art\\" + GetArtLocation() + ".ico, 0");
            File.SetAttributes(albumini, FileAttributes.System | FileAttributes.Hidden);

            foreach (var subalbum in SubAlbums)
            {
                foreach (var message in subalbum.Save())
                {
                    yield return "\t" + message;
                }
            }
            foreach (var song in Songs)
            {
                foreach (var message in song.Save())
                {
                    yield return "\t" + message;
                }
            }
        }

        public IEnumerable<Song> AllSongs()
        {
            return Songs.Concat(SubAlbums.SelectMany(x => x.Songs));
        }
    }

    public class SubAlbum : IHaveArt
    {
        public List<Song> Songs;
        public string Folder;
        public string Name;
        public string FolderName;
        public Album ParentAlbum;
        public Artist ParentArtist;
        public TagLib.Picture Art;
        public SubAlbum(string folder, Album parent)
        {
            ParentAlbum = parent;
            ParentArtist = parent.Parent;
            Folder = folder;
            FolderName = Path.GetFileName(Folder);
            Name = NameRetriever.GetName(FolderName);
            Art = ArtRetriever.GetArt(GetArtLocation());
            Songs = new List<Song>();
            foreach (var song in Directory.EnumerateFiles(folder, "*.mp3"))
            {
                Songs.Add(new Song(song, this));
            }
        }

        public string GetArtLocation()
        {
            return Path.Combine(ParentArtist.FolderName, ParentAlbum.FolderName, FolderName);
        }

        public IEnumerable<string> Save()
        {
            yield return $"SUBALBUM: {Name}";
            yield return $"ART: {GetArtLocation()}";

            string subalbumini = Path.Combine(Folder, "desktop.ini");
            File.Delete(subalbumini);
            File.WriteAllText(subalbumini, "[.ShellClassInfo]\nIconResource = ..\\..\\..\\.music-cache\\art\\" + GetArtLocation() + ".ico, 0");
            File.SetAttributes(subalbumini, FileAttributes.System | FileAttributes.Hidden);

            foreach (var song in Songs)
            {
                foreach (var message in song.Save())
                {
                    yield return "\t" + message;
                }
            }
        }
    }

    public class Song
    {
        // external read-only access to tag title (only accessible after calling Save)
        public string Title;
        public string Filepath;
        public string Filename => Path.GetFileNameWithoutExtension(Filepath);
        // possibly null if direct child of real album
        public SubAlbum ParentSubAlbum;
        // direct parent or parent of subalbum
        public Album ParentAlbum;
        public Artist ParentArtist;
        public Song(string path, SubAlbum parent) : this(path)
        {
            ParentSubAlbum = parent;
            ParentAlbum = parent.ParentAlbum;
            ParentArtist = ParentAlbum.Parent;
        }
        public Song(string path, Album parent) : this(path)
        {
            ParentAlbum = parent;
            ParentArtist = parent.Parent;
        }
        private Song(string path)
        {
            Filepath = path;
        }

        private string GetIdealName()
        {
            return NameRetriever.GetName(Path.GetFileNameWithoutExtension(Filepath), correctcase: true);
        }

        private string GetIdealFilename()
        {
            return NameRetriever.GetSafeFileName(GetIdealName()) + Path.GetExtension(Filepath);
        }

        private TagLib.Picture[] GetPictures()
        {
            var pictures = new List<TagLib.Picture>();
            foreach (var artpath in GetAllArtLocations())
            {
                var art = ArtRetriever.GetArt(artpath);
                if (art != null)
                    pictures.Add(art);
            }
            return pictures.ToArray();
        }

        private string[] GetAllArtLocations()
        {
            var paths = new List<string>();
            if (ParentSubAlbum != null)
                paths.Add(ParentSubAlbum.GetArtLocation());
            paths.Add(ParentAlbum.GetArtLocation());
            paths.Add(ParentArtist.GetArtLocation());
            return paths.ToArray();
        }

        private static bool CompareArt(TagLib.IPicture[] pictures1, TagLib.IPicture[] pictures2)
        {
            if (pictures1.Length != pictures2.Length)
                return false;
            for (int i = 0; i < pictures1.Length; i++)
            {
                if (pictures1[i].Data != pictures2[i].Data)
                    return false;
            }
            return true;
        }

        // returns whether this changed anything
        private Tuple<bool, IEnumerable<string>> WipeUselessProperties(TagLib.Tag filetag)
        {
            List<string> messages = new List<string>();
            bool changed = false;
            if (filetag.AmazonId != null)
            {
                messages.Add($"Wiped amazon ID {filetag.AmazonId}");
                filetag.AmazonId = null;
                changed = true;
            }
            if (filetag.Comment != null)
            {
                messages.Add($"Wiped comment {filetag.Comment}");
                filetag.Comment = null;
                changed = true;
            }
            if (filetag.Conductor != null)
            {
                messages.Add($"Wiped conductor {filetag.Conductor}");
                filetag.Conductor = null;
                changed = true;
            }
            if (filetag.Copyright != null)
            {
                messages.Add($"Wiped copyright {filetag.Copyright}");
                filetag.Copyright = null;
                changed = true;
            }
            if (filetag.Disc != 0)
            {
                messages.Add($"Wiped disc number {filetag.Disc}");
                filetag.Disc = 0;
                changed = true;
            }
            if (filetag.DiscCount != 0)
            {
                messages.Add($"Wiped disc count {filetag.DiscCount}");
                filetag.DiscCount = 0;
                changed = true;
            }
            if (filetag.FirstGenre != null)
            {
                messages.Add($"Wiped genre {filetag.FirstGenre}");
                filetag.Genres = new string[0];
                changed = true;
            }
            if (filetag.Lyrics != null)
            {
                messages.Add($"Wiped lyrics {filetag.Lyrics}");
                filetag.Lyrics = null;
                changed = true;
            }
            if (filetag.MusicBrainzArtistId != null || filetag.MusicBrainzDiscId != null || filetag.MusicBrainzReleaseArtistId != null || filetag.MusicBrainzReleaseCountry != null || filetag.MusicBrainzReleaseId != null || filetag.MusicBrainzReleaseStatus != null || filetag.MusicBrainzReleaseType != null || filetag.MusicBrainzTrackId != null)
            {
                messages.Add($"Wiped musicbrainz data");
                filetag.MusicBrainzArtistId = null;
                filetag.MusicBrainzDiscId = null;
                filetag.MusicBrainzReleaseArtistId = null;
                filetag.MusicBrainzReleaseCountry = null;
                filetag.MusicBrainzReleaseId = null;
                filetag.MusicBrainzReleaseStatus = null;
                filetag.MusicBrainzReleaseType = null;
                filetag.MusicBrainzTrackId = null;
                changed = true;
            }
            if (filetag.MusicIpId != null)
            {
                messages.Add($"Wiped music IP ID {filetag.MusicIpId}");
                filetag.MusicIpId = null;
                changed = true;
            }
            if (filetag.Track != 0)
            {
                messages.Add($"Wiped track number {filetag.Track}");
                filetag.Track = 0;
                changed = true;
            }
            if (filetag.TrackCount != 0)
            {
                messages.Add($"Wiped track count {filetag.TrackCount}");
                filetag.TrackCount = 0;
                changed = true;
            }
            if (filetag.Year != 0)
            {
                messages.Add($"Wiped year {filetag.Year}");
                filetag.Year = 0;
                changed = true;
            }
            return Tuple.Create<bool, IEnumerable<string>>(changed, messages);
        }

        public IEnumerable<string> Save()
        {
            // file name (includes extension and placeholder chars like underscore)
            string originalname = Path.GetFileName(Filepath);
            string newname = originalname;
            // the song TITLE should be this
            yield return "SONG: " + originalname;
            bool check = false;
            if (ModifiedOptimizer.FileDifferent(Filepath, true))
            {
                check = true;
                yield return "(possible change to song)";
            }
            if (!check)
            {
                foreach (var image in GetAllArtLocations())
                {
                    if (ModifiedOptimizer.FileDifferent(ArtRetriever.FullLocation(image), false))
                    {
                        check = true;
                        yield return "(possible change to art)";
                    }
                }
            }
            if (!check)
                yield break;
            ModifiedOptimizer.RecordChange(Filepath);
            using (TagLib.File file = TagLib.File.Create(Filepath))
            {
                Title = file.Tag.Title;
                bool changed = false;
                // don't ruin existing titles with non-file characters like slashes
                string newtitle = GetIdealName();
                if (file.Tag.Title != newtitle)
                {
                    file.Tag.Title = newtitle;
                    changed = true;
                    yield return $"New title: {file.Tag.Title}";
                }
                newname = GetIdealFilename();
                if (file.Tag.Album != ParentAlbum.Name)
                {
                    file.Tag.Album = ParentAlbum.Name;
                    changed = true;
                    yield return $"New album: {ParentAlbum.Name}";
                }
                if (file.Tag.FirstAlbumArtist != ParentArtist.Name)
                {
                    file.Tag.AlbumArtists = new string[] { ParentArtist.Name };
                    changed = true;
                    yield return $"New album artist: {ParentArtist.Name}";
                }
                if (file.Tag.FirstComposer != ParentArtist.Name)
                {
                    file.Tag.Composers = new string[] { ParentArtist.Name };
                    changed = true;
                    yield return $"New composer: {ParentArtist.Name}";
                }
                if (file.Tag.FirstPerformer != ParentArtist.Name)
                {
                    file.Tag.Performers = new string[] { ParentArtist.Name };
                    changed = true;
                    yield return $"New performer: {ParentArtist.Name}";
                }
                if (file.Tag.Grouping != ParentSubAlbum?.Name)
                {
                    if (ParentSubAlbum?.Name == null)
                        yield return $"Wiped subalbum {file.Tag.Grouping}";
                    else
                        yield return $"New subalbum: {ParentSubAlbum?.Name}";
                    file.Tag.Grouping = ParentSubAlbum?.Name;
                    changed = true;
                }
                // get pictures in order of priority (subalbum, then album, then artist)
                var pictures = GetPictures();
                if (!CompareArt(file.Tag.Pictures, pictures))
                {
                    // delete existing pictures if there are any, but subalbum/album/artist don't have one
                    if (pictures.Length == 0)
                    {
                        yield return "No suitable album/artist art found, deleted embedded album art";
                        file.Tag.Pictures = new TagLib.IPicture[0];
                    }
                    else
                    {
                        file.Tag.Pictures = pictures;
                        yield return $"New embedded album art *{pictures.Length}";
                    }
                    changed = true;
                }
                // order matters here because the method must always run, even if we have already changed something
                var wipe = WipeUselessProperties(file.Tag);
                foreach (var message in wipe.Item2)
                {
                    yield return "\t" + message;
                }
                changed = wipe.Item1 || changed;

                if (changed)
                {
                    bool savefail = false;
                    yield return "Saving...";
                    try
                    {
                        file.Save();
                    }
                    catch (IOException)
                    {
                        savefail = true;
                    }
                    if (savefail)
                        yield return "Save failed! Skipping...";
                }
            }
            if (newname != originalname)
            {
                yield return $"New name requires new file path: \"{originalname}\" to \"{newname}\"";
                string newpath = Path.Combine(Path.GetDirectoryName(Filepath), newname);
                File.Move(Filepath, newpath);
                Filepath = newpath;
            }
        }
    }
}
