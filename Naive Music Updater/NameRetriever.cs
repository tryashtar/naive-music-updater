using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public static class NameRetriever
    {
        private static List<string> SkipNames;
        private static List<string> LowercaseWords;
        private static Dictionary<string, string> NameMap;
        private static Dictionary<string, string> FindReplace;
        private static Dictionary<string, string> FileToTitle;
        private static Dictionary<string, string> TitleToFile;
        static NameRetriever()
        {
            SkipNames = new List<string>();
            LowercaseWords = new List<string>();
            NameMap = new Dictionary<string, string>();
            FindReplace = new Dictionary<string, string>();
            FileToTitle = new Dictionary<string, string>();
            TitleToFile = new Dictionary<string, string>();
        }
        public static void LoadConfig(string configpath)
        {
            JObject json = JObject.Parse(File.ReadAllText(configpath));
            foreach (var skip in json["skip"])
            {
                SkipNames.Add((string)skip);
            }
            foreach (var lower in json["lowercase"])
            {
                LowercaseWords.Add((string)lower);
            }
            foreach (var map in (JObject)json["map"])
            {
                NameMap.Add(map.Key, (string)map.Value);
            }
            foreach (var map in (JObject)json["find_replace"])
            {
                FindReplace.Add(map.Key, (string)map.Value);
            }
            foreach (var map in (JObject)json["filename_to_title"])
            {
                FileToTitle.Add(map.Key, (string)map.Value);
            }
            foreach (var map in (JObject)json["title_to_filename"])
            {
                TitleToFile.Add(map.Key, (string)map.Value);
            }
        }
        public static string GetName(string name, bool correctcase = false)
        {
            // 1. configurations
            foreach (var skip in SkipNames)
            {
                if (String.Equals(skip, name, StringComparison.OrdinalIgnoreCase))
                    return skip;
            }
            if (NameMap.TryGetValue(name, out string result))
                return result;
            foreach (var filenamechar in FileToTitle)
            {
                name = name.Replace(filenamechar.Key, filenamechar.Value);
            }
            foreach (var findrepl in FindReplace)
            {
                name = name.Replace(findrepl.Key, findrepl.Value);
            }
            // 2. corrections
            if (correctcase)
                name = CorrectCase(name);
            return name;
        }

        public static string GetSafeFileName(string name)
        {
            foreach (var filenamechar in TitleToFile)
            {
                name = name.Replace(filenamechar.Key, filenamechar.Value);
            }
            return name;
        }

        public static string CorrectCase(string input)
        {
            if (input == "")
                return input;
            // remove whitespace from beginning and end
            input = input.Trim();

            // turn double-spaces into single spaces
            input = Regex.Replace(input, @"\s+", " ");

            // treat parenthesized phrases like a title
            int left = input.IndexOf('(');
            string spacebefore = (left > 0 && input[left - 1] == ' ') ? " " : "";
            int right = input.IndexOf(')');
            string spaceafter = (right < input.Length - 1 && input[right + 1] == ' ') ? " " : "";
            if (left != -1 && right != -1)
            {
                // a bit naive, but hey...
                return CorrectCase(input.Substring(0, left)) + spacebefore + "(" +
                    CorrectCase(input.Substring(left + 1, right - left - 1)) + ")" + spaceafter +
                    CorrectCase(input.Substring(right + 1, input.Length - right - 1));
            }

            // treat "artist - title" style titles as two separate titles
            foreach (var separator in new string[] { "-", "–", "—", "_", "/", "!", ":" })
            {
                string spaced;
                if (separator == ":" || separator == "!")
                    spaced = $"{separator} ";
                else
                {
                    spaced = $" {separator} ";
                    if (input.StartsWith(separator))
                        input = " " + input;
                }
                string[] titles = input.Split(new[] { spaced }, StringSplitOptions.None);
                if (titles.Length > 1)
                {
                    for (int i = 0; i < titles.Length; i++)
                    {
                        titles[i] = CorrectCase(titles[i]);
                    }

                    // all internal titles have already been processed, we are done
                    input = String.Join(spaced, titles);
                }
                if (titles.Length > 1)
                    return input.Trim();
            }

            string[] words = input.Split(' ');
            words[0] = Char.ToUpper(words[0][0]) + words[0].Substring(1);
            words[words.Length - 1] = Char.ToUpper(words[words.Length - 1][0]) + words[words.Length - 1].Substring(1);
            bool prevallcaps = false;
            for (int i = 1; i < words.Length - 1; i++)
            {
                bool allcaps = words[i] == words[i].ToUpper();
                if (!(allcaps && prevallcaps) && AlwaysLowercase(words[i]))
                    words[i] = Char.ToLower(words[i][0]) + words[i].Substring(1);
                else
                    words[i] = Char.ToUpper(words[i][0]) + words[i].Substring(1);
                prevallcaps = allcaps;
            }
            return String.Join(" ", words);
        }

        private static bool AlwaysLowercase(string word)
        {
            string nopunc = new String(word.Where(c => !Char.IsPunctuation(c)).ToArray());
            return LowercaseWords.Contains(nopunc.ToLower());
        }
    }
}
