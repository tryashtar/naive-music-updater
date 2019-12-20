﻿using System;
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
                    byte[] bytes = ConvertToIcon(image, true);
                    if (!File.Exists(ico) || !File.ReadAllBytes(ico).SequenceEqual(bytes))
                        File.WriteAllBytes(ico, ConvertToIcon(image, true));
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
            if (!ConvertToIcon(inputStream, outputStream, 256, preserveAspectRatio))
                return null;
            return outputStream.ToArray();
        }

        private static bool ConvertToIcon(Stream input, Stream output, int size = 256, bool preserveAspectRatio = false)
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
        private static Dictionary<string, DateTime> OriginalCache;
        private static Dictionary<string, DateTime> CurrentCache;
        private static string CachePath;

        // parse cache for last modified dates
        public static void LoadCache(string filepath)
        {
            CachePath = filepath;
            string json = File.ReadAllText(filepath);
            OriginalCache = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(json);
            CurrentCache = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(json);
        }

        // returns true if this file has changed since the last time we recorded it
        public static bool FileDifferent(string filepath, bool result_if_no_exist)
        {
            if (!File.Exists(filepath))
                return result_if_no_exist;
            DateTime modified = File.GetLastWriteTime(filepath);
            DateTime created = File.GetCreationTime(filepath);
            var date = modified > created ? modified : created;
            if (CurrentCache.TryGetValue(filepath, out DateTime cached))
                return date - TimeSpan.FromSeconds(5) > cached;
            else
                return true;
        }

        // mark this file as having been changed right now
        public static void RecordChange(string filepath)
        {
            CurrentCache[filepath] = DateTime.Now;
        }

        public static void UnrecordChange(string filepath)
        {
            bool exists = OriginalCache.TryGetValue(filepath, out var time);
            if (exists)
                CurrentCache[filepath] = time;
            else
                CurrentCache.Remove(filepath);
        }

        public static void SaveCache()
        {
            File.WriteAllText(CachePath, JsonConvert.SerializeObject(CurrentCache));
        }
    }

    public static class NameRetriever
    {
        private static List<string> SkipNames;
        private static List<string> LowercaseWords;
        private static Dictionary<string, string> NameMap;
        private static Dictionary<string, string> FindReplace;
        private static Dictionary<string, string> FileToTitle;
        private static Dictionary<string, string> TitleToFile;
        static NameRetriever()
        {
            SkipNames = new List<string>();
            LowercaseWords = new List<string>();
            NameMap = new Dictionary<string, string>();
            FindReplace = new Dictionary<string, string>();
            FileToTitle = new Dictionary<string, string>();
            TitleToFile = new Dictionary<string, string>();
        }
        public static void LoadConfig(string configpath)
        {
            JObject json = JObject.Parse(File.ReadAllText(configpath));
            foreach (var skip in json["skip"])
            {
                SkipNames.Add((string)skip);
            }
            foreach (var lower in json["lowercase"])
            {
                LowercaseWords.Add((string)lower);
            }
            foreach (var map in (JObject)json["map"])
            {
                NameMap.Add(map.Key, (string)map.Value);
            }
            foreach (var map in (JObject)json["find_replace"])
            {
                FindReplace.Add(map.Key, (string)map.Value);
            }
            foreach (var map in (JObject)json["filename_to_title"])
            {
                FileToTitle.Add(map.Key, (string)map.Value);
            }
            foreach (var map in (JObject)json["title_to_filename"])
            {
                TitleToFile.Add(map.Key, (string)map.Value);
            }
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
            foreach (var filenamechar in FileToTitle)
            {
                name = name.Replace(filenamechar.Key, filenamechar.Value);
            }

            // 2. corrections
            if (correctcase)
                name = CorrectCase(name);
            foreach (var findrepl in FindReplace)
            {
                name = name.Replace(findrepl.Key, findrepl.Value);
            }
            return name;
        }

        public static string GetSafeFileName(string name)
        {
            foreach (var filenamechar in TitleToFile)
            {
                name = name.Replace(filenamechar.Key, filenamechar.Value);
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
            foreach (var separator in new string[] { "-", "–", "—", "_", "/", "!", ":" })
            {
                bool starts = input.StartsWith(separator);
                if (starts)
                    input = " " + input;
                string spaced = $" {separator} ";
                string[] titles = input.Split(new[] { spaced }, StringSplitOptions.RemoveEmptyEntries);
                if (titles.Length == 1)
                    continue;
                for (int i = 0; i < titles.Length; i++)
                {
                    titles[i] = CorrectCase(titles[i]);
                }

                // all internal titles have already been processed, we are done
                input = String.Join(spaced, titles);
                if (starts)
                    input = input.Substring(1);
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
            return LowercaseWords.Contains(nopunc.ToLower());
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

        public void Save()
        {
            foreach (var artist in Artists)
            {
                artist.Save();
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

        public void Save()
        {
            Logger.WriteLine($"ARTIST: {Name}");
            Logger.WriteLine($"ART: {GetArtLocation()}");

            string artistini = Path.Combine(Folder, "desktop.ini");
            File.Delete(artistini);
            File.WriteAllText(artistini, "[.ShellClassInfo]\nIconResource = ..\\.music-cache\\art\\" + GetArtLocation() + ".ico, 0");
            File.SetAttributes(artistini, FileAttributes.System | FileAttributes.Hidden);

            foreach (var album in Albums)
            {
                album.Save();
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

        public void Save()
        {
            Logger.WriteLine($"ALBUM: {Name}");
            Logger.WriteLine($"ART: {GetArtLocation()}");

            string albumini = Path.Combine(Folder, "desktop.ini");
            File.Delete(albumini);
            File.WriteAllText(albumini, "[.ShellClassInfo]\nIconResource = ..\\..\\.music-cache\\art\\" + GetArtLocation() + ".ico, 0");
            File.SetAttributes(albumini, FileAttributes.System | FileAttributes.Hidden);

            foreach (var subalbum in SubAlbums)
            {
                subalbum.Save();
            }
            foreach (var song in Songs)
            {
                song.Save();
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
                song.Save();
            }
        }
    }

    public class Song
    {
        // external read-only access to tag title (only accessible after calling Save)
        public string Title;
        public string Filepath;
        public string Filename => Path.GetFileNameWithoutExtension(Filepath);
        public string SubFilename => ParentSubAlbum == null ? Filename : ParentSubAlbum.FolderName + "/" + Filename;
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
                {
                    pictures.Add(art);
                    break;
                }
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
                messages.Add($"FYI -- this song has lyrics");
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

        public void Save()
        {
            // file name (includes extension and placeholder chars like underscore)
            string originalname = Path.GetFileName(Filepath);
            string newname = originalname;
            // the song TITLE should be this
            Logger.WriteLine("SONG: " + originalname);
            bool check = false;
            if (ModifiedOptimizer.FileDifferent(Filepath, true))
            {
                check = true;
                Logger.WriteLine("(possible change to song)");
            }
            if (!check)
            {
                foreach (var image in GetAllArtLocations())
                {
                    if (ModifiedOptimizer.FileDifferent(ArtRetriever.FullLocation(image), false))
                    {
                        check = true;
                        Logger.WriteLine("(possible change to art)");
                    }
                }
            }
            if (!check)
                return;
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
                    Logger.WriteLine($"New title: {file.Tag.Title}");
                }
                newname = GetIdealFilename();
                if (file.Tag.Album != ParentAlbum.Name)
                {
                    file.Tag.Album = ParentAlbum.Name;
                    changed = true;
                    Logger.WriteLine($"New album: {ParentAlbum.Name}");
                }
                if (file.Tag.FirstAlbumArtist != ParentArtist.Name)
                {
                    file.Tag.AlbumArtists = new string[] { ParentArtist.Name };
                    changed = true;
                    Logger.WriteLine($"New album artist: {ParentArtist.Name}");
                }
                if (file.Tag.FirstComposer != ParentArtist.Name)
                {
                    file.Tag.Composers = new string[] { ParentArtist.Name };
                    changed = true;
                    Logger.WriteLine($"New composer: {ParentArtist.Name}");
                }
                if (file.Tag.FirstPerformer != ParentArtist.Name)
                {
                    file.Tag.Performers = new string[] { ParentArtist.Name };
                    changed = true;
                    Logger.WriteLine($"New performer: {ParentArtist.Name}");
                }
                if (file.Tag.Grouping != ParentSubAlbum?.Name)
                {
                    if (ParentSubAlbum?.Name == null)
                        Logger.WriteLine($"Wiped subalbum {file.Tag.Grouping}");
                    else
                        Logger.WriteLine($"New subalbum: {ParentSubAlbum?.Name}");
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
                        Logger.WriteLine("No suitable album/artist art found, deleted embedded album art");
                        file.Tag.Pictures = new TagLib.IPicture[0];
                    }
                    else
                    {
                        file.Tag.Pictures = pictures;
                        Logger.WriteLine($"New embedded album art *{pictures.Length}");
                    }
                    changed = true;
                }
                // order matters here because the method must always run, even if we have already changed something
                var wipe = WipeUselessProperties(file.Tag);
                foreach (var message in wipe.Item2)
                {
                    Logger.WriteLine("\t" + message);
                }
                changed = wipe.Item1 || changed;

                if (changed)
                {
                    IOException exc = null;
                    Logger.WriteLine("Saving...");
                    try
                    {
                        file.Save();
                    }
                    catch (IOException ex)
                    {
                        exc = ex;
                    }
                    if (exc != null)
                    {
                        ModifiedOptimizer.UnrecordChange(Filepath);
                        Logger.WriteLine($"Save failed because {exc.Message}! Skipping...");
                    }
                }
            }
            if (newname != originalname)
            {
                Logger.WriteLine($"New name requires new file path: \"{originalname}\" to \"{newname}\"");
                string newpath = Path.Combine(Path.GetDirectoryName(Filepath), newname);
                File.Move(Filepath, newpath);
                Filepath = newpath;
            }
        }
    }
}
