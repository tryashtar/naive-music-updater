﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib.Id3v2;
using YamlDotNet.RepresentationModel;

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
        private readonly Dictionary<string, IMetadataStrategy> NamedStrategies = new Dictionary<string, IMetadataStrategy>();
        private readonly string MP3GainPath;
        private readonly string MP3GainArgs;
        private readonly string MetaFlacPath;
        private readonly string MetaFlacArgs;
        private readonly List<string> IllegalPrivateOwners;
        private readonly List<string> KeepFrameIDs;
        private readonly List<Regex> KeepFrameTexts;
        public readonly int SourceAutoMaxDistance;
        public LibraryConfig(string file)
        {
            if (!File.Exists(file))
            {
                Logger.WriteLine($"Couldn't find config file {file}, using blank config!!!");
                return;
            }
            var yaml = YamlHelper.ParseFile(file);
            foreach (string item in (YamlSequenceNode)yaml["lowercase"])
            {
                LowercaseWords.Add(item.ToLower());
            }
            foreach (string item in (YamlSequenceNode)yaml["skip"])
            {
                SkipNames.Add(item);
            }
            foreach (var item in (YamlMappingNode)yaml["find_replace"])
            {
                FindReplace.Add((string)item.Key, (string)item.Value);
            }
            foreach (var item in (YamlMappingNode)yaml["map"])
            {
                MapNames.Add((string)item.Key, (string)item.Value);
            }
            foreach (var item in (YamlMappingNode)yaml["title_to_filename"])
            {
                FilesafeConversions.Add((string)item.Key, (string)item.Value);
            }
            foreach (var item in (YamlMappingNode)yaml["title_to_foldername"])
            {
                FoldersafeConversions.Add((string)item.Key, (string)item.Value);
            }
            foreach (var item in (YamlMappingNode)yaml["strategies"]["named"])
            {
                NamedStrategies.Add((string)item.Key, MetadataStrategyFactory.Create(item.Value));
            }
            var mp3path = yaml.Go("replay_gain", "mp3", "path");
            var mp3args = yaml.Go("replay_gain", "mp3", "args");
            var flacpath = yaml.Go("replay_gain", "flac", "path");
            var flacargs = yaml.Go("replay_gain", "flac", "args");
            var cpo = yaml.Go("clear_private_owners");
            var samd = yaml.Go("source_auto_max_distance");
            var keep_frames = yaml.Go("keep_frames", "ids");
            var keep_text = yaml.Go("keep_frames", "text");
            if (mp3path != null)
                MP3GainPath = (string)mp3path;
            if (mp3args != null)
                MP3GainArgs = (string)mp3args;
            if (flacpath != null)
                MetaFlacPath = (string)flacpath;
            if (flacargs != null)
                MetaFlacArgs = (string)flacargs;
            if (cpo != null)
                IllegalPrivateOwners = cpo.ToStringList();
            if (samd != null)
                SourceAutoMaxDistance = int.Parse((string)samd);
            if (keep_frames != null)
                KeepFrameIDs = keep_frames.ToStringList();
            if (keep_text != null)
                KeepFrameTexts = keep_text.ToList(x => new Regex((string)x));
        }

        public IMetadataStrategy GetNamedStrategy(string name)
        {
            return NamedStrategies[name];
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

        public bool ShouldKeepFrame(TextInformationFrame frame)
        {
            var id = frame.FrameId.ToString();
            if (KeepFrameIDs.Contains(id))
                return true;
            var text = frame.Text.First();
            if (KeepFrameTexts.Any(x => x.IsMatch(text)))
                return true;
            return false;
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
            var process = new Process();
            var extension = Path.GetExtension(song.Location);
            if (extension == ".mp3")
                process.StartInfo = new ProcessStartInfo(MP3GainPath, $"{MP3GainArgs} \"{location}\"");
            else if (extension == ".flac")
                process.StartInfo = new ProcessStartInfo(MetaFlacPath, $"{MetaFlacArgs} \"{location}\"");
            process.StartInfo.UseShellExecute = false;
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