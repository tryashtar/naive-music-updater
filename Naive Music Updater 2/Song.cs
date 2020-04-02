using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib.Id3v2;

namespace NaiveMusicUpdater
{
    // to do:
    // - art
    // - write lyrics again
    // - wipe pointless entries again
    // - folders can rename themselves too after getting their title/filename
    // - implement GetArtistFor and GetAlbumFor (maybe tricky)...
    // - config has "strategies" for getting artist/album/etc, default is parent[1]/parent[0] title
    // - mp3gain again
    public class Song : IMusicItem
    {
        public string Location { get; private set; }
        protected readonly MusicFolder Parent;
        public Song(MusicFolder parent, string file)
        {
            Parent = parent;
            Location = file;
        }

        public void Update(LibraryCache cache)
        {
            Logger.WriteLine($"Song: {SimpleName}");
            if (!cache.NeedsUpdate(this))
                return;
            Logger.WriteLine($"(Changed recently)");
            var title = cache.GetTitleFor(this);
            var artist = cache.GetArtistFor(this);
            var album = cache.GetAlbumFor(this);
            using (TagLib.File file = TagLib.File.Create(Location))
            {
                bool done = true;
                bool changed = UpdateTag(file.Tag, title, artist, album);
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
                    cache.MarkUpdatedRecently(this);
            }
            var filename = cache.ToFilesafe(title);
            if (SimpleName != filename)
            {
                Logger.WriteLine($"Changing filename to {filename}");
                var newpath = Path.Combine(Path.GetDirectoryName(Location), Path.ChangeExtension(filename, Path.GetExtension(Location)));
                File.Move(Location, newpath);
                Location = newpath;
            }
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

        private static bool IsSingleValue(string[] array, string value)
        {
            if (array == null)
                return value == null;
            if (value == null)
                return false;
            if (array.Length > 1)
                return false;
            return array[0] == value;
        }

        public string SimpleName => Path.GetFileNameWithoutExtension(this.Location);

        public IEnumerable<string> PathFromRoot()
        {
            var list = new List<string>();
            if (this.Parent != null)
                list.AddRange(this.Parent.PathFromRoot());
            list.Add(this.SimpleName);
            return list;
        }
    }
}
