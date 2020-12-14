using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib.Id3v2;

namespace NaiveMusicUpdater
{
    public class LibraryConfig
    {
        private readonly HashSet<string> LowercaseWords = new HashSet<string>();
        private readonly HashSet<string> SkipNames = new HashSet<string>();
        private readonly Dictionary<string, string> FindReplace = new Dictionary<string, string>();
        private readonly Dictionary<string, string> MapNames = new Dictionary<string, string>();
        private readonly Dictionary<string, string> FilesafeConversions = new Dictionary<string, string>();
        private readonly Dictionary<string, string> FoldersafeConversions = new Dictionary<string, string>();
        private readonly IMetadataStrategy DefaultStrategy;
        private readonly Dictionary<string, IMetadataStrategy> NamedStrategies = new Dictionary<string, IMetadataStrategy>();
        private readonly List<(List<SongPredicate>, IMetadataStrategy)> StrategyOverrides = new List<(List<SongPredicate>, IMetadataStrategy)>();
        private readonly string MP3GainPath;
        private readonly List<string> IllegalPrivateOwners;
        public LibraryConfig(string file)
        {
            if (!File.Exists(file))
            {
                Logger.WriteLine($"Couldn't find config file {file}, using blank config!!!");
                DefaultStrategy = new NoOpMetadataStrategy();
                return;
            }
            var json = JObject.Parse(File.ReadAllText(file));
            foreach (string item in (JArray)json["lowercase"])
            {
                LowercaseWords.Add(item.ToLower());
            }
            foreach (string item in (JArray)json["skip"])
            {
                SkipNames.Add(item);
            }
            foreach (var item in (JObject)json["find_replace"])
            {
                FindReplace.Add(item.Key, (string)item.Value);
            }
            foreach (var item in (JObject)json["map"])
            {
                MapNames.Add(item.Key, (string)item.Value);
            }
            foreach (var item in (JObject)json["title_to_filename"])
            {
                FilesafeConversions.Add(item.Key, (string)item.Value);
            }
            foreach (var item in (JObject)json["title_to_foldername"])
            {
                FoldersafeConversions.Add(item.Key, (string)item.Value);
            }
            DefaultStrategy = MetadataStrategyFactory.Create(this, json["strategies"]["default"]);
            foreach (var item in (JObject)json["strategies"]["named"])
            {
                NamedStrategies.Add(item.Key, MetadataStrategyFactory.Create(this, item.Value));
            }
            foreach (JObject item in (JArray)json["strategies"]["overrides"])
            {
                var predicates = new List<SongPredicate>();
                if (item.TryGetValue("name", out var name))
                    predicates.Add(new SongPredicate((string)name));
                else if (item.TryGetValue("names", out var names))
                    predicates.AddRange(((JArray)names).Select(x => new SongPredicate((string)x)));
                if (item.TryGetValue("reference", out var reference))
                    StrategyOverrides.Add((predicates, NamedStrategies[(string)reference]));
                if (item.TryGetValue("set", out var set))
                    StrategyOverrides.Add((predicates, MetadataStrategyFactory.Create(this, set)));
            }
            json.TryGetValue("mp3gain_path", out var mp3path);
            if (mp3path.Type == JTokenType.String)
                MP3GainPath = (string)mp3path;
            json.TryGetValue("clear_private_owners", out var cpo);
            if (cpo.Type == JTokenType.Array)
                IllegalPrivateOwners = cpo.ToObject<List<string>>();
        }

        private IEnumerable<IMetadataStrategy> GetApplicableStrategies(IMusicItem item)
        {
            yield return DefaultStrategy;
            foreach (var (predicates, strategy) in StrategyOverrides)
            {
                if (predicates.Any(x => x.Matches(item)))
                {
                    yield return strategy;
                }
            }
        }

        public bool IsIllegalPrivateOwner(string owner)
        {
            if (IllegalPrivateOwners == null)
                return false;
            foreach (var item in IllegalPrivateOwners)
            {
                if (Regex.IsMatch(owner, item))
                    return true;
            }
            return false;
        }

        public SongMetadata GetMetadataFor(IMusicItem item)
        {
            var merged = new MultipleMetadataStrategy(GetApplicableStrategies(item));
            return merged.Perform(item);
        }

        public string CleanName(string name)
        {
            foreach (var skip in SkipNames)
            {
                if (String.Equals(skip, name, StringComparison.OrdinalIgnoreCase))
                    return skip;
            }
            if (MapNames.TryGetValue(name, out string result))
                return result;
            name = FindReplaceName(name);
            name = CorrectCase(name);
            name = FindReplaceName(name);
            return name;
        }

        private string FindReplaceName(string name)
        {
            foreach (var findrepl in FindReplace)
            {
                name = name.Replace(findrepl.Key, findrepl.Value);
            }
            return name;
        }

        public string ToFilesafe(string text, bool isfolder)
        {
            var conv = isfolder ? FoldersafeConversions : FilesafeConversions;
            foreach (var filenamechar in conv)
            {
                text = text.Replace(filenamechar.Key, filenamechar.Value);
            }
            if (isfolder)
            {
                text = RemoveDiacritics(text);
                text = text.TrimEnd('.');
            }
            return text;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public bool NormalizeAudio(Song song)
        {
            if (MP3GainPath == null)
                return true;
            string location = song.Location;
            bool abnormal_chars = song.Location.Any(x => x > 255);
            string temp_file = Path.Combine(Path.GetTempPath(), "temp-song" + Path.GetExtension(song.Location));
            string text_file = Path.Combine(Path.GetDirectoryName(song.Location), "temp-song-original.txt");
            if (abnormal_chars)
            {
                Logger.WriteLine("Weird characters detected, doing weird rename thingy");
                File.WriteAllText(text_file, song.Location + "\n" + temp_file);
                location = temp_file;
                if (File.Exists(location))
                    throw new InvalidOperationException("That's not supposed to be there...");
                File.Move(song.Location, location);
            }
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo(MP3GainPath, $"/r /c \"{location}\"")
                {
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
            if (abnormal_chars)
            {
                File.Move(location, song.Location);
                File.Delete(text_file);
            }
            return process.ExitCode == 0;
        }

        public string CorrectCase(string text)
        {
            if (text == "")
                return text;

            // remove whitespace from beginning and end
            text = text.Trim();

            // turn double-spaces into single spaces
            text = Regex.Replace(text, @"\s+", " ");

            // treat "foo (bar)" like two titles
            var parens = Regex.Match(text, @"^(.*) \((.+)\)$");
            if (parens.Success)
            {
                var part1 = parens.Groups[1].Value;
                var part2 = parens.Groups[2].Value;
                return $"{CorrectCase(part1)} ({CorrectCase(part2)})";
            }

            // treat "foo - bar" like two titles
            var hyphens = Regex.Match(text, @"^(.+) (-|–|—|_|\/) (.+)$");
            if (hyphens.Success)
            {
                var part1 = hyphens.Groups[1].Value;
                var part2 = hyphens.Groups[2].Value;
                var part3 = hyphens.Groups[3].Value;
                return $"{CorrectCase(part1)} {part2} {CorrectCase(part3)}";
            }

            // treat "foo! bar" like two titles
            var exclam = Regex.Match(text, @"^(.+)(!|\?|:|\.|,|;) (.+)$");
            if (exclam.Success)
            {
                var part1 = exclam.Groups[1].Value;
                var part2 = exclam.Groups[2].Value;
                var part3 = exclam.Groups[3].Value;
                return $"{CorrectCase(part1)}{part2} {CorrectCase(part3)}";
            }

            string[] words = text.Split(' ');
            // capitalize first and last words of title always
            Capitalize(words, 0);
            Capitalize(words, words.Length - 1);
            bool prevallcaps = (words[0] == words[0].ToUpper());
            for (int i = 1; i < words.Length - 1; i++)
            {
                bool allcaps = words[i] == words[i].ToUpper();
                if (!(allcaps && prevallcaps) && IsLowercase(words[i]))
                    Lowercase(words, i);
                else
                    Capitalize(words, i);
                prevallcaps = allcaps;
            }
            return String.Join(" ", words);
        }

        private bool IsLowercase(string word)
        {
            string nopunc = new String(word.Where(c => !Char.IsPunctuation(c)).ToArray());
            return LowercaseWords.Contains(nopunc.ToLower());
        }

        private static void Capitalize(string[] input, int index)
        {
            input[index] = Char.ToUpper(input[index][0]) + input[index].Substring(1);
        }

        private static void Lowercase(string[] input, int index)
        {
            input[index] = Char.ToLower(input[index][0]) + input[index].Substring(1);
        }
    }
}
