using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using TagLib.Id3v2;
using File = System.IO.File;

namespace NaiveMusicUpdater
{
    public class Song : IMusicItem
    {
        public string Location { get; private set; }
        protected readonly MusicFolder _Parent;
        public MusicFolder Parent => _Parent;
        public Song(MusicFolder parent, string file)
        {
            _Parent = parent;
            Location = file;
        }

        public void Update(LibraryCache cache)
        {
            Logger.WriteLine($"Song: {SimpleName}");
            if (!cache.NeedsUpdate(this))
                return;
            Logger.WriteLine($"(Changed recently)");
            var metadata = cache.GetMetadataFor(this);
            var title = metadata.Title;
            var artist = metadata.Artist;
            var album = metadata.Album;
            using (TagLib.File file = TagLib.File.Create(Location))
            {
                bool done = true;
                bool changed = UpdateTag(file.Tag, title, artist, album);
                changed |= UpdateArt(file.Tag, cache.GetArtPathFor(this));
                changed |= WipeUselessProperties(file.Tag);
                Logger.WriteLine("Checking for lyrics...");
                var path = Util.StringPathAfterRoot(this);
                changed |= cache.WriteLyrics(path, (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2));
                if (changed)
                {
                    Logger.WriteLine("Saving...");
                    try { file.Save(); }
                    catch (IOException ex)
                    {
                        Logger.WriteLine($"Save failed because {ex.Message}! Skipping...");
                        done = false;
                    }
                }
                if (done)
                {
                    Logger.WriteLine("Normalizing audio with MP3gain...");
                    cache.Normalize(this);
                }
                if (done)
                    cache.MarkUpdatedRecently(this);
            }
            var filename = cache.ToFilesafe(title, false);
            if (SimpleName != filename)
            {
                Logger.WriteLine($"Changing filename to {filename}");
                var newpath = Path.Combine(Path.GetDirectoryName(Location), Path.ChangeExtension(filename, Path.GetExtension(Location)));
                File.Move(Location, newpath);
                Location = newpath;
            }
        }

        private bool UpdateArt(TagLib.Tag tag, string art_path)
        {
            var picture = art_path == null ? null : ArtCache.GetPicture(art_path);
            if (!IsSingleValue(tag.Pictures, picture))
            {
                if (picture == null)
                {
                    if (tag.Pictures.Length == 0)
                        return false;
                    Logger.WriteLine($"Deleting art");
                    tag.Pictures = new IPicture[0];
                }
                else
                {
                    Logger.WriteLine($"Changing art");
                    tag.Pictures = new IPicture[] { picture };
                }
                return true;
            }
            return false;
        }

        private bool UpdateTag(TagLib.Tag tag, string title, string artist, string album)
        {
            bool changed = false;
            if (tag.Title != title)
            {
                Logger.WriteLine($"Changing title to {title}");
                tag.Title = title;
                changed = true;
            }
            if (tag.Album != album)
            {
                Logger.WriteLine($"Changing album to {album}");
                tag.Album = album;
                changed = true;
            }
            if (!IsSingleValue(tag.AlbumArtists, artist))
            {
                Logger.WriteLine($"Changing album artists to {artist}");
                tag.AlbumArtists = new string[] { artist };
                changed = true;
            }
            if (!IsSingleValue(tag.Composers, artist))
            {
                Logger.WriteLine($"Changing composers to {artist}");
                tag.Composers = new string[] { artist };
                changed = true;
            }
            if (!IsSingleValue(tag.Performers, artist))
            {
                Logger.WriteLine($"Changing performers to {artist}");
                tag.Performers = new string[] { artist };
                changed = true;
            }
            return changed;
        }

        // returns whether this changed anything
        private bool WipeUselessProperties(TagLib.Tag filetag)
        {
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
            return changed;
        }

        private static bool IsSingleValue<T>(T[] array, T value)
        {
            if (array == null)
                return value == null;
            if (value == null)
                return false;
            if (array.Length != 1)
                return false;
            return value.Equals(array[0]);
        }

        private static bool IsSingleValue(IPicture[] array, IPicture value)
        {
            if (array == null)
                return value == null;
            if (value == null)
                return false;
            if (array.Length != 1)
                return false;

            return value.Data == array[0].Data;
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


        public string SimpleName => Path.GetFileNameWithoutExtension(this.Location);

        public IEnumerable<IMusicItem> PathFromRoot()
        {
            var list = new List<IMusicItem>();
            if (this._Parent != null)
                list.AddRange(this._Parent.PathFromRoot());
            list.Add(this);
            return list;
        }
    }
}
