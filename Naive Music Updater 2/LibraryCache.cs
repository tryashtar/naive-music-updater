using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                return date - TimeSpan.FromSeconds(5) > cached;
            else
                return true;
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
    }
}
