using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
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
}
