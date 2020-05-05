using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class MusicLibrary : MusicFolder
    {
        private LibraryCache Cache;
        public MusicLibrary(string folder) : base(folder)
        {
            var cache = GetCacheFolder();
            Logger.Open(Path.Combine(cache, "logs", DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss") + ".txt"));
            Cache = new LibraryCache(cache);
        }

        private string GetCacheFolder() => Path.Combine(Location, ".music-cache");

        public void Update()
        {
            Update(Cache);
            Cache.Save();
        }
    }
}
