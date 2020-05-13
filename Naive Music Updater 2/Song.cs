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
            Logger.WriteLine($"(checking)");
            var metadata = cache.GetMetadataFor(this);
            var title = metadata.Title;
            var artist = metadata.Artist;
            var album = metadata.Album;
            var comment = metadata.Comment;
            using (TagLib.File file = TagLib.File.Create(Location))
            {
                bool done = true;
                bool changed = UpdateTag(file.Tag, title, artist, album, comment);
                changed |= UpdateArt(file.Tag, cache.GetArtPathFor(this));
                changed |= WipeUselessProperties(file.Tag);
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
                    cache.NormalizeAudio(this);
                    cache.MarkUpdatedRecently(this);
                }
            }
            // correct case of filename
            // changing filename in other ways is forbidden because stuff is derived from it
            // it's the USER's job to set the filename they want and set config to determine how to pull metadata out of it
            var filename = cache.ToFilesafe(cache.CleanName(SimpleName), false);
            if (SimpleName != filename)
            {
                Logger.WriteLine($"Renaming file: \"{filename}\"");
                var newpath = Path.Combine(Path.GetDirectoryName(Location), filename + Path.GetExtension(Location));
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
                    Logger.WriteLine($"Deleted art");
                    tag.Pictures = new IPicture[0];
                }
                else
                {
                    Logger.WriteLine($"Added art");
                    tag.Pictures = new IPicture[] { picture };
                }
                return true;
            }
            return false;
        }

        private void ChangedThing(string thing, object old_value, object new_value)
        {
            if (old_value != null)
                Logger.WriteLine($"Deleted {thing}: \"{old_value}\"");
            if (new_value != null)
                Logger.WriteLine($"Added {thing}: \"{new_value}\"");
        }

        private void ChangedThing(string thing, IEnumerable<string> old_value, string new_value)
        {
            if (old_value != null)
                Logger.WriteLine($"Deleted {thing}: \"{String.Join(";", old_value)}\"");
            if (new_value != null)
                Logger.WriteLine($"Added {thing}: \"{new_value}\"");
        }

        private bool UpdateTag(TagLib.Tag tag, string title, string artist, string album, string comment)
        {
            bool changed = false;
            if (tag.Title != title)
            {
                ChangedThing("title", tag.Title, title);
                tag.Title = title;
                changed = true;
            }
            if (tag.Album != album)
            {
                ChangedThing("album", tag.Album, album);
                tag.Album = album;
                changed = true;
            }
            if (tag.Comment != comment)
            {
                ChangedThing("comment", tag.Comment, comment);
                tag.Comment = comment;
                changed = true;
            }
            if (!IsSingleValue(tag.AlbumArtists, artist))
            {
                ChangedThing("album artists", tag.AlbumArtists, artist);
                tag.AlbumArtists = new string[] { artist };
                changed = true;
            }
            if (!IsSingleValue(tag.Composers, artist))
            {
                ChangedThing("composers", tag.Composers, artist);
                tag.Composers = new string[] { artist };
                changed = true;
            }
            if (!IsSingleValue(tag.Performers, artist))
            {
                ChangedThing("performers", tag.Performers, artist);
                tag.Performers = new string[] { artist };
                changed = true;
            }
            return changed;
        }

        // returns whether this changed anything
        private bool WipeUselessProperties(TagLib.Tag tag)
        {
            bool changed = false;
            if (tag.AmazonId != null)
            {
                ChangedThing("amazon id", tag.AmazonId, null);
                tag.AmazonId = null;
                changed = true;
            }
            if (tag.Conductor != null)
            {
                ChangedThing("conductor", tag.Conductor, null);
                tag.Conductor = null;
                changed = true;
            }
            if (tag.Copyright != null)
            {
                ChangedThing("copyright", tag.Copyright, null);
                tag.Copyright = null;
                changed = true;
            }
            if (tag.Disc != 0)
            {
                ChangedThing("disc number", tag.Disc, null);
                tag.Disc = 0;
                changed = true;
            }
            if (tag.DiscCount != 0)
            {
                ChangedThing("disc count", tag.DiscCount, null);
                tag.DiscCount = 0;
                changed = true;
            }
            if (tag.FirstGenre != null)
            {
                ChangedThing("genre", tag.Genres, null);
                tag.Genres = new string[0];
                changed = true;
            }
            if (tag.MusicBrainzArtistId != null || tag.MusicBrainzDiscId != null || tag.MusicBrainzReleaseArtistId != null || tag.MusicBrainzReleaseCountry != null || tag.MusicBrainzReleaseId != null || tag.MusicBrainzReleaseStatus != null || tag.MusicBrainzReleaseType != null || tag.MusicBrainzTrackId != null)
            {
                ChangedThing("musicbrainz data", new string[] { tag.MusicBrainzArtistId,
                tag.MusicBrainzDiscId,
                tag.MusicBrainzReleaseArtistId,
                tag.MusicBrainzReleaseCountry,
                tag.MusicBrainzReleaseId,
                tag.MusicBrainzReleaseStatus,
                tag.MusicBrainzReleaseType,
                tag.MusicBrainzTrackId, }, null);

                tag.MusicBrainzArtistId = null;
                tag.MusicBrainzDiscId = null;
                tag.MusicBrainzReleaseArtistId = null;
                tag.MusicBrainzReleaseCountry = null;
                tag.MusicBrainzReleaseId = null;
                tag.MusicBrainzReleaseStatus = null;
                tag.MusicBrainzReleaseType = null;
                tag.MusicBrainzTrackId = null;
                changed = true;
            }
            if (tag.MusicIpId != null)
            {
                ChangedThing("music IP", tag.MusicIpId, null);
                tag.MusicIpId = null;
                changed = true;
            }
            if (tag.Track != 0)
            {
                ChangedThing("track number", tag.Track, 0);
                tag.Track = 0;
                changed = true;
            }
            if (tag.TrackCount != 0)
            {
                ChangedThing("track count", tag.TrackCount, 0);
                tag.TrackCount = 0;
                changed = true;
            }
            if (tag.Year != 0)
            {
                ChangedThing("year", tag.Year, 0);
                tag.Year = 0;
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
