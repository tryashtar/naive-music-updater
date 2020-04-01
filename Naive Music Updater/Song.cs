using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib.Id3v2;

namespace NaiveMusicUpdater
{
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

        private string GetIdealFilename()
        {
            return NameRetriever.GetSafeFileName(NameRetriever.GetName(Path.GetFileNameWithoutExtension(this.Filepath), correctcase: true));
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
        private bool WipeUselessProperties(TagLib.Tag filetag)
        {
            Logger.TabIn();
            bool changed = false;
            if (filetag.AmazonId != null)
            {
                Logger.WriteLine($"Wiped amazon ID {filetag.AmazonId}");
                filetag.AmazonId = null;
                changed = true;
            }
            if (filetag.Comment != null)
            {
                Logger.WriteLine($"Wiped comment {filetag.Comment}");
                filetag.Comment = null;
                changed = true;
            }
            if (filetag.Conductor != null)
            {
                Logger.WriteLine($"Wiped conductor {filetag.Conductor}");
                filetag.Conductor = null;
                changed = true;
            }
            if (filetag.Copyright != null)
            {
                Logger.WriteLine($"Wiped copyright {filetag.Copyright}");
                filetag.Copyright = null;
                changed = true;
            }
            if (filetag.Disc != 0)
            {
                Logger.WriteLine($"Wiped disc number {filetag.Disc}");
                filetag.Disc = 0;
                changed = true;
            }
            if (filetag.DiscCount != 0)
            {
                Logger.WriteLine($"Wiped disc count {filetag.DiscCount}");
                filetag.DiscCount = 0;
                changed = true;
            }
            if (filetag.FirstGenre != null)
            {
                Logger.WriteLine($"Wiped genre {filetag.FirstGenre}");
                filetag.Genres = new string[0];
                changed = true;
            }
            if (filetag.MusicBrainzArtistId != null || filetag.MusicBrainzDiscId != null || filetag.MusicBrainzReleaseArtistId != null || filetag.MusicBrainzReleaseCountry != null || filetag.MusicBrainzReleaseId != null || filetag.MusicBrainzReleaseStatus != null || filetag.MusicBrainzReleaseType != null || filetag.MusicBrainzTrackId != null)
            {
                Logger.WriteLine($"Wiped musicbrainz data");
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
                Logger.WriteLine($"Wiped music IP ID {filetag.MusicIpId}");
                filetag.MusicIpId = null;
                changed = true;
            }
            if (filetag.Track != 0)
            {
                Logger.WriteLine($"Wiped track number {filetag.Track}");
                filetag.Track = 0;
                changed = true;
            }
            if (filetag.TrackCount != 0)
            {
                Logger.WriteLine($"Wiped track count {filetag.TrackCount}");
                filetag.TrackCount = 0;
                changed = true;
            }
            if (filetag.Year != 0)
            {
                Logger.WriteLine($"Wiped year {filetag.Year}");
                filetag.Year = 0;
                changed = true;
            }
            Logger.TabOut();
            return changed;
        }

        public void Save(string cachefolder)
        {
            // file name (includes extension and placeholder chars like underscore)
            string current_file_name = Path.GetFileName(Filepath);
            // the song TITLE should be this
            Logger.WriteLine("SONG: " + current_file_name);
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
                string proper_file_name = GetIdealFilename() + Path.GetExtension(this.Filepath);
                string proper_title = NameRetriever.GetName(Path.GetFileNameWithoutExtension(proper_file_name), correctcase: true);

                if (proper_file_name != current_file_name)
                {
                    Logger.WriteLine($"New name requires new file path: \"{current_file_name}\" to \"{proper_file_name}\"");
                    var test = GetIdealFilename();
                    string newpath = Path.Combine(Path.GetDirectoryName(Filepath), proper_file_name);
                    File.Move(Filepath, newpath);
                    Filepath = newpath;
                }

                if (file.Tag.Title != proper_title)
                {
                    file.Tag.Title = proper_title;
                    changed = true;
                    Logger.WriteLine($"New title: {file.Tag.Title}");
                }
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
                if (!String.IsNullOrEmpty(file.Tag.Lyrics))
                {
                    Logger.WriteLine($"FYI -- this song has lyrics");
                    SynchedText[] lyrics = null;
                    var tag = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2);
                    if (tag != null)
                    {
                        foreach (var frame in tag.GetFrames())
                        {
                            if (frame is SynchronisedLyricsFrame slf)
                                lyrics = slf.Text;
                        }
                    }
                    if (lyrics == null)
                        lyrics = new SynchedText[] { new SynchedText(0, tag.Lyrics) };
                    var lyricstext = lyrics.Select(x => $"[{TimeSpan.FromMilliseconds(x.Time).ToString(@"h\:mm\:ss\.ff")}]{x.Text}");
                    string[] paths;
                    if (this.ParentSubAlbum != null)
                        paths = new[] { cachefolder, "lyrics", this.ParentArtist.FolderName, this.ParentAlbum.FolderName, this.ParentSubAlbum?.FolderName, Path.ChangeExtension(proper_file_name, ".lrc") };
                    else
                        paths = new[] { cachefolder, "lyrics", this.ParentArtist.FolderName, this.ParentAlbum.FolderName, Path.ChangeExtension(proper_file_name, ".lrc") };
                    var location = Path.Combine(paths);
                    Directory.CreateDirectory(Path.GetDirectoryName(location));
                    File.WriteAllLines(location, lyricstext);
                }
                // order matters here because the method must always run, even if we have already changed something
                var wipe = WipeUselessProperties(file.Tag);
                changed = wipe || changed;

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
            Logger.WriteLine("Normalizing audio with MP3gain...");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo(@"C:\Program Files (x86)\MP3Gain\mp3gain.exe", $"/r /c \"{Filepath}\"")
                {
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }
}
