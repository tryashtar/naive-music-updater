using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

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
            string sourcesfile = Path.Combine(this.Location, "sources.yaml");
            var sources = (YamlMappingNode)YamlHelper.ParseFile(sourcesfile);

            AddBlankSources(sources, this);
            CheckSources(sources, this);

            YamlHelper.SaveToFile(sources, sourcesfile);
        }

        private void AddBlankSources(YamlMappingNode obj, MusicFolder folder)
        {
            Logger.TabIn();
            foreach (var item in folder.SubFolders)
            {
                var token = (YamlMappingNode)obj.TryGet(item.SimpleName);
                if (token == null)
                {
                    token = new YamlMappingNode();
                    obj.Add(item.SimpleName, token);
                    Logger.WriteLine($"Added new folder to sources: {item.SimpleName}");
                }
                AddBlankSources(token, item);
            }
            Logger.TabOut();
        }

        private void CheckSources(YamlMappingNode obj, MusicFolder folder)
        {
            Logger.WriteLine(folder.SimpleName);
            Logger.TabIn();
            List<string> no_sources;
            var no_sources_token = (YamlSequenceNode)obj.TryGet("?");
            if (no_sources_token == null)
                no_sources = new List<string>();
            else
                no_sources = YamlHelper.ToStringArray(no_sources_token).ToList();
            var songs = folder.Songs.Select(x => x.SimpleName).ToList();
            foreach (var item in obj)
            {
                if ((string)item.Key == "?")
                    continue;
                if (item.Value.NodeType == YamlNodeType.Sequence || item.Value.NodeType == YamlNodeType.Scalar)
                {
                    // this is a song source
                    string[] sourced = item.Value is YamlSequenceNode j ? YamlHelper.ToStringArray(j) : new string[] { (string)item.Value };

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
                    var associated_folder = folder.SubFolders.FirstOrDefault(x => x.SimpleName == (string)item.Key);
                    if (associated_folder == null)
                        Logger.WriteLine($"Folder in sources but not library: {item.Key}");
                    else
                        CheckSources((YamlMappingNode)item.Value, associated_folder);
                }
            }
            foreach (var song in songs)
            {
                Logger.WriteLine($"Song missing source: {song}");
                no_sources.Add(song);
            }
            no_sources = no_sources.Distinct().ToList();
            if (no_sources.Any())
                obj.Add("MISSING", new YamlSequenceNode(no_sources.Select(x => new YamlScalarNode(x))));
            else
                obj.Children.Remove("MISSING");
            Logger.TabOut();
        }
    }
}
