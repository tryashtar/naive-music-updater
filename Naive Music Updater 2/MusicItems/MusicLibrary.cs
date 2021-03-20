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
                var token = (YamlMappingNode)obj.Go(item.SimpleName);
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

        private const string MISSING_SOURCE = "MISSING";
        private void CheckSources(YamlMappingNode obj, MusicFolder folder)
        {
            Logger.WriteLine(folder.SimpleName);
            Logger.TabIn();

            var reverse_sources = new Dictionary<string, YamlNode>();
            var songs_to_check = folder.Songs.Select(x => x.SimpleName).ToList();
            var redundant_songs = new List<string>();
            var conversions = new Dictionary<string, string>();

            foreach (var item in obj)
            {
                if ((string)item.Key == MISSING_SOURCE)
                    continue;
                if (item.Value.NodeType == YamlNodeType.Sequence || item.Value.NodeType == YamlNodeType.Scalar)
                {
                    // this is a song source
                    string[] sourced = item.Value is YamlSequenceNode j ? YamlHelper.ToStringList(j).ToArray() : new string[] { (string)item.Value };

                    foreach (string song in sourced)
                    {
                        reverse_sources[song] = item.Value;
                        if (songs_to_check.Contains(song))
                            songs_to_check.Remove(song);
                        else
                        {
                            Logger.WriteLine($"Song in sources but not library: {song}");
                            redundant_songs.Add(song);
                        }
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
            foreach (var song in songs_to_check)
            {
                Logger.WriteLine($"Song missing source: {song}");
                if (redundant_songs.Any())
                {
                    var search = MinLevenshtein(song, redundant_songs.Except(conversions.Values));
                    if (search != null && search.Value.distance <= Cache.Config.SourceAutoMaxDistance)
                        conversions[song] = search.Value.result;
                }
            }
            if (conversions.Any() || redundant_songs.Any())
            {
                Logger.WriteLine("");
                Logger.WriteLine("Possible source autocorrections:");
                Logger.TabIn();
                foreach (var item in redundant_songs)
                {
                    Logger.WriteLine("X " + item);
                }
                foreach (var item in conversions)
                {
                    Logger.WriteLine("┎ " + item.Value);
                    Logger.WriteLine("┖ " + item.Key);
                }
                Logger.TabOut();
                Logger.WriteLine("Accept?");
                var input = Logger.ReadLine();
                if (input == "")
                    Logger.WriteLine("Skipped");
                else
                {
                    Logger.WriteLine("Converting...");
                    foreach (var item in conversions)
                    {
                        songs_to_check.Remove(item.Key);
                        var redundant_location = reverse_sources[item.Value];
                        if (redundant_location is YamlScalarNode simple)
                            simple.Value = item.Key;
                        else if (redundant_location is YamlSequenceNode list)
                        {
                            int index = list.Children.IndexOf(list.Children.First(x => (string)x == item.Value));
                            list.Children[index] = new YamlScalarNode(item.Key);
                        }
                    }
                    foreach (var item in redundant_songs)
                    {
                        var redundant_location = reverse_sources[item];
                        if (redundant_location is YamlScalarNode simple)
                            obj.Children.Remove(item);
                        else if (redundant_location is YamlSequenceNode list)
                        {
                            int index = list.Children.IndexOf(list.Children.First(x => (string)x == item));
                            list.Children.RemoveAt(index);
                        }
                    }
                }
            }
            songs_to_check = songs_to_check.Distinct().ToList();
            obj.Children.Remove(MISSING_SOURCE);
            if (songs_to_check.Any())
                obj.Add(MISSING_SOURCE, new YamlSequenceNode(songs_to_check.Select(x => new YamlScalarNode(x))));
            Logger.TabOut();
        }

        private static (string result, int distance)? MinLevenshtein(string template, IEnumerable<string> options)
        {
            string result = null;
            int min = int.MaxValue;
            foreach (var item in options)
            {
                var distance = CalcLevenshteinDistance(template, item);
                if (distance < min)
                {
                    min = distance;
                    result = item;
                }
            }
            if (result == null)
                return null;
            return (result, min);
        }

        private static int CalcLevenshteinDistance(string a, string b)
        {
            if (String.IsNullOrEmpty(a) && String.IsNullOrEmpty(b))
            {
                return 0;
            }
            if (String.IsNullOrEmpty(a))
            {
                return b.Length;
            }
            if (String.IsNullOrEmpty(b))
            {
                return a.Length;
            }
            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (int i = 1; i <= lengthA; i++)
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                    distances[i, j] = Math.Min
                        (
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost
                        );
                }
            return distances[lengthA, lengthB];
        }
    }
}
