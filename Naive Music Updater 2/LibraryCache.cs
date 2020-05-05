using Newtonsoft.Json;
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
    public class LibraryCache
    {
        private string Folder;
        private LibraryConfig Config;
        private Dictionary<string, DateTime> DateCache;
        private Dictionary<IMusicItem, SongMetadata> MetadataCache = new Dictionary<IMusicItem, SongMetadata>();
        private string DateCachePath => Path.Combine(Folder, "datecache.json");
        private string ConfigPath => Path.Combine(Folder, "config.json");
        public LibraryCache(string folder)
        {
            Folder = folder;
            Config = new LibraryConfig(ConfigPath);
            var datecache = File.ReadAllText(DateCachePath);
            DateCache = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(datecache);
        }

        public void Save()
        {
            foreach (var item in ArtCache.Cached.Keys)
            {
                DateCache[item] = DateTime.Now;
            }
            File.WriteAllText(DateCachePath, JsonConvert.SerializeObject(DateCache));
        }

        public bool NeedsUpdate(IMusicItem item)
        {
            var location = item.Location;
            if (!File.Exists(location))
                return false;
            DateTime modified = File.GetLastWriteTime(location);
            DateTime created = File.GetCreationTime(location);
            var date = modified > created ? modified : created;
            if (DateCache.TryGetValue(location, out DateTime cached))
            {
                if (date - TimeSpan.FromSeconds(5) > cached)
                    return true;
                var art = GetArtPathFor(item);
                if (art == null)
                    return false;
                if (DateCache.TryGetValue(art, out DateTime cached2))
                {
                    if (date - TimeSpan.FromSeconds(5) > cached2)
                        return true;
                }
                else
                    return true;
                return false;
            }
            else
                return true;
        }

        public string GetArtPathFor(IMusicItem item)
        {
            while (item != null)
            {
                var partial = Util.StringPathAfterRoot(item);
                if (item is Song)
                    partial = Path.ChangeExtension(partial, null);
                var path = Path.Combine(Folder, "art", partial + ".png");
                if (File.Exists(path))
                    return path;
                item = item.Parent;
            }
            return null;
        }

        public void MarkUpdatedRecently(IMusicItem item)
        {
            DateCache[item.Location] = DateTime.Now;
        }

        public SongMetadata GetMetadataFor(IMusicItem song)
        {
            if (MetadataCache.TryGetValue(song, out var result))
                return result;
            var meta = Config.GetMetadataFor(song);
            MetadataCache[song] = meta;
            return meta;
        }

        public string ToFilesafe(string text, bool isfolder)
        {
            return Config.ToFilesafe(text, isfolder);
        }

        public bool Normalize(Song song)
        {
            return Config.Normalize(song);
        }

        // returns true if tag was changed and needs to be resaved
        public bool WriteLyrics(string location, TagLib.Id3v2.Tag tag)
        {
            bool changed = false;
            SynchedText[] lyrics = null;
            if (tag != null)
            {
                foreach (var frame in tag.GetFrames())
                {
                    if (frame is SynchronisedLyricsFrame slf)
                    {
                        lyrics = slf.Text;
                        break;
                    }
                }
            }
            if (lyrics != null)
            {
                Logger.WriteLine("Found synchronized lyrics tag");
                var plain_lyrics = String.Join("\n", lyrics.Select(x => x.Text));
                // update lyrics tag
                if (tag.Lyrics != plain_lyrics)
                {
                    Logger.WriteLine("Updating plain lyrics tag");
                    tag.Lyrics = plain_lyrics;
                    changed = true;
                }
            }
            else
            {
                if (tag.Lyrics == null)
                    return false;
                Logger.WriteLine("Found simple lyrics");
                Logger.WriteLine("Adding synchronized lyrics tag");
                // convert to frame
                lyrics = new SynchedText[] { new SynchedText(0, tag.Lyrics) };
                var frame = new SynchronisedLyricsFrame("lyrics", "english", SynchedTextType.Lyrics, TagLib.StringType.Latin1);
                frame.Text = lyrics;
                tag.AddFrame(frame);
                changed = true;
            }
            var lyricstext = lyrics.Select(x => $"[{TimeSpan.FromMilliseconds(x.Time):h\\:mm\\:ss\\.ff}]{x.Text}");
            location = Path.ChangeExtension(Path.Combine(Folder, "lyrics", location), ".lrc");
            bool write_file = true;
            if (File.Exists(location))
            {
                var existing = File.ReadAllLines(location);
                if (existing.SequenceEqual(lyricstext))
                    write_file = false;
            }
            if (write_file)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(location));
                File.WriteAllLines(location, lyricstext);
            }
            return changed;
        }
    }
}
