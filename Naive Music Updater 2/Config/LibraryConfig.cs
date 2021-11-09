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
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class LibraryConfig
    {
        private readonly HashSet<string> LowercaseWords;
        private readonly HashSet<string> SkipNames;
        private readonly Dictionary<string, string> FindReplace;
        private readonly Dictionary<string, string> MapNames;
        private readonly Dictionary<string, string> FilesafeConversions;
        private readonly Dictionary<string, string> FoldersafeConversions;
        private readonly Dictionary<string, IMetadataStrategy> NamedStrategies;
        private readonly string MP3GainPath;
        private readonly string MP3GainArgs;
        private readonly string MetaFlacPath;
        private readonly string MetaFlacArgs;
        private readonly string AACGainPath;
        private readonly string AACGainArgs;
        private readonly List<string> IllegalPrivateOwners;
        private readonly List<string> KeepFrameIDs;
        private readonly List<string> SongExtensions;
        private readonly List<Regex> KeepFrameTexts;
        private readonly List<Regex> TitleSplits;
        public readonly int SourceAutoMaxDistance;

        public LibraryConfig(string file)
        {
            YamlNode yaml;
            if (File.Exists(file))
                yaml = YamlHelper.ParseFile(file);
            else
            {
                Logger.WriteLine($"Couldn't find config file {file}, using blank config!!!");
                yaml = new YamlMappingNode();
            }

            LowercaseWords = yaml.Go("lowercase").ToListFromStrings(x => x.ToLower())?.ToHashSet() ?? new();
            SkipNames = yaml.Go("skip").ToList()?.ToHashSet() ?? new();
            FindReplace = yaml.Go("find_replace").ToDictionary() ?? new();
            MapNames = yaml.Go("map").ToDictionary() ?? new();
            FilesafeConversions = yaml.Go("title_to_filename").ToDictionary() ?? new();
            FoldersafeConversions = yaml.Go("title_to_foldername").ToDictionary() ?? new();
            NamedStrategies = yaml.Go("named_strategies").ToDictionary(x => MetadataStrategyFactory.Create(x)) ?? new();
            IllegalPrivateOwners = yaml.Go("clear_private_owners").ToList() ?? new();
            KeepFrameIDs = yaml.Go("keep_frames", "ids").ToList() ?? new();
            KeepFrameTexts = yaml.Go("keep_frames", "text").ToListFromStrings(x => new Regex(x)) ?? new();
            TitleSplits = yaml.Go("title_splits").ToListFromStrings(x => new Regex(x)) ?? new();
            SongExtensions = yaml.Go("extensions").ToListFromStrings(x => x.StartsWith('.') ? x.ToLower() : "." + x.ToLower()) ?? new();

            SourceAutoMaxDistance = yaml.Go("source_auto_max_distance").Int() ?? 0;
            var mp3_gain = yaml.Go("replay_gain", "mp3");
            MP3GainPath = mp3_gain.Go("path").String();
            MP3GainArgs = mp3_gain.Go("args").String();
            var flac_gain = yaml.Go("replay_gain", "flac");
            MetaFlacPath = flac_gain.Go("path").String();
            MetaFlacArgs = flac_gain.Go("args").String();
            var aac_gain = yaml.Go("replay_gain", "aac");
            AACGainPath = flac_gain.Go("path").String();
            AACGainArgs = flac_gain.Go("args").String();
        }

        public bool IsSongFile(string file)
        {
            string extension = Path.GetExtension(file).ToLower();
            return SongExtensions.Contains(extension);
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
            else if (extension == ".m4a")
                process.StartInfo = new ProcessStartInfo(AACGainPath, $"{AACGainArgs} \"{location}\"");
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

            foreach (var title in TitleSplits)
            {
                var match = title.Match(text);
                if (match.Success)
                {
                    var result = "";
                    foreach (var group in ((IEnumerable<Group>)match.Groups).Skip(1))
                    {
                        if (group.Name.EndsWith("_title"))
                            result += CorrectCase(group.Value);
                        else if (group.Name.EndsWith("_skip"))
                            result += group.Value;
                    }
                    return result;
                }
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
