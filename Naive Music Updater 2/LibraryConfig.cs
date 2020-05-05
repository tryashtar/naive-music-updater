﻿using Newtonsoft.Json.Linq;
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
        private HashSet<string> LowercaseWords = new HashSet<string>();
        private HashSet<string> SkipNames = new HashSet<string>();
        private Dictionary<string, string> FindReplace = new Dictionary<string, string>();
        private Dictionary<string, string> MapNames = new Dictionary<string, string>();
        private Dictionary<string, string> FilesafeConversions = new Dictionary<string, string>();
        private Dictionary<string, string> FoldersafeConversions = new Dictionary<string, string>();
        private MetadataStrategy DefaultStrategy;
        private List<Tuple<SongPredicate, MetadataStrategy>> StrategyOverrides = new List<Tuple<SongPredicate, MetadataStrategy>>();
        private string MP3GainPath;
        public LibraryConfig(string file)
        {
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
            DefaultStrategy = new MetadataStrategy(this, (JObject)json["strategies"]["default"]);
            foreach (var item in (JObject)json["strategies"]["overrides"])
            {
                StrategyOverrides.Add(Tuple.Create(new SongPredicate(item.Key), new MetadataStrategy(this, (JObject)item.Value)));
            }
            json.TryGetValue("mp3gain_path", out var mp3path);
            if (mp3path.Type == JTokenType.String)
                MP3GainPath = (string)mp3path;
        }

        private IEnumerable<MetadataStrategy> GetApplicableStrategies(IMusicItem item)
        {
            yield return DefaultStrategy;
            foreach (var strat in StrategyOverrides)
            {
                if (strat.Item1.Matches(item))
                    yield return strat.Item2;
            }
        }

        public SongMetadata GetMetadataFor(IMusicItem item)
        {
            SongMetadata metadata = default;
            foreach (var strategy in GetApplicableStrategies(item))
            {
                var extra = strategy.Perform(item);
                metadata = metadata.Combine(extra);
            }
            return metadata;
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
            foreach (var findrepl in FindReplace)
            {
                name = name.Replace(findrepl.Key, findrepl.Value);
            }
            name = CorrectCase(name);
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
                text = RemoveDiacritics(text);
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

        public bool Normalize(Song song)
        {
            if (MP3GainPath == null)
                return true;
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo(MP3GainPath, $"/r /c \"{song.Location}\"")
                {
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
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
            var exclam = Regex.Match(text, @"^(.+)(!|\?|:) (.+)$");
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