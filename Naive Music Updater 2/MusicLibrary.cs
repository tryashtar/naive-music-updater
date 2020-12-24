using Newtonsoft.Json.Linq;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class MusicLibrary : MusicFolder
    {
        protected readonly LibraryCache Cache;
        public override LibraryCache GlobalCache => Cache;
        public MusicLibrary(string folder) : base(folder)
        {
            var cache = GetCacheFolder();
            Cache = new LibraryCache(cache);
        }

        private string GetCacheFolder() => Path.Combine(Location, ".music-cache");

        public void UpdateLibrary()
        {
            Logger.Open(Path.Combine(GetCacheFolder(), "logs", DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss") + ".txt"));
            foreach (var child in ChildFolders)
            {
                child.Update();
            }
            Cache.Save();
        }

        public void UpdateSources()
        {
            Logger.WriteLine("Start sources scan");
            // prepare to scan sources
            string sourcesjson = Path.Combine(this.Location, "sources.json");
            var sources = JObject.Parse(File.ReadAllText(sourcesjson));

            AddBlankSources(sources, this);
            CheckSources(sources, this);

            File.WriteAllText(sourcesjson, sources.ToString());
        }

        private void AddBlankSources(JObject obj, MusicFolder folder)
        {
            Logger.TabIn();
            foreach (var item in folder.SubFolders)
            {
                if (!obj.TryGetValue(item.SimpleName, out var token))
                {
                    token = new JObject();
                    obj.Add(item.SimpleName, token);
                    Logger.WriteLine($"Added new folder to sources: {item.SimpleName}");
                }
                AddBlankSources((JObject)token, item);
            }
            Logger.TabOut();
        }

        private void CheckSources(JObject obj, MusicFolder folder)
        {
            Logger.WriteLine(folder.SimpleName);
            Logger.TabIn();
            List<string> no_sources;
            if (obj.TryGetValue("", out var no_sources_token))
                no_sources = no_sources_token.ToObject<List<string>>();
            else
                no_sources = new List<string>();
            var songs = folder.Songs.Select(x => x.SimpleName).ToList();
            foreach (var item in obj)
            {
                if (item.Key == "")
                    continue;
                if (item.Value.Type == JTokenType.Array || item.Value.Type == JTokenType.String)
                {
                    // this is a song source
                    string[] sourced = item.Value is JArray j ? j.ToObject<string[]>() : new string[] { (string)item.Value };

                    foreach (string song in sourced)
                    {
                        if (songs.Contains(song))
                        {
                            songs.Remove(song);
                            no_sources.Remove(song);
                        }
                        else
                            Logger.WriteLine($"Song in sources but not library: {song}");
                    }
                }
                else
                {
                    // this is a folder object
                    var associated_folder = folder.SubFolders.FirstOrDefault(x => x.SimpleName == item.Key);
                    if (associated_folder == null)
                        Logger.WriteLine($"Folder in sources but not library: {item.Key}");
                    else
                        CheckSources((JObject)item.Value, associated_folder);
                }
            }
            foreach (var song in songs)
            {
                Logger.WriteLine($"Song missing source: {song}");
                no_sources.Add(song);
            }
            no_sources = no_sources.Distinct().ToList();
            if (no_sources.Any())
                obj[""] = new JArray(no_sources);
            else
                obj.Remove("");
            Logger.TabOut();
        }
    }
}
