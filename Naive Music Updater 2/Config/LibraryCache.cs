using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib;
using TagLib.Id3v2;
using YamlDotNet.Serialization;
using File = System.IO.File;

namespace NaiveMusicUpdater
{
    public class LibraryCache
    {
        public readonly string Folder;
        public readonly LibraryConfig Config;
        private readonly Dictionary<string, DateTime> DateCache;
        private readonly Dictionary<string, DateTime> PendingDateCache;
        private string DateCachePath => Path.Combine(Folder, "datecache.yaml");
        private string ConfigPath => Path.Combine(Folder, "library.yaml");
        public LibraryCache(string folder)
        {
            Folder = folder;
            Config = new LibraryConfig(ConfigPath);
            if (File.Exists(DateCachePath))
            {
                var datecache = File.ReadAllText(DateCachePath);
                var deserializer = new DeserializerBuilder().Build();
                DateCache = deserializer.Deserialize<Dictionary<string, DateTime>>(datecache) ?? new Dictionary<string, DateTime>();
                PendingDateCache = new Dictionary<string, DateTime>(DateCache);
            }
            else
            {
                Logger.WriteLine($"Couldn't find date cache {DateCachePath}, starting fresh");
                DateCache = new Dictionary<string, DateTime>();
                PendingDateCache = new Dictionary<string, DateTime>();
            }
        }

        public void Save()
        {
            foreach (var item in ArtCache.Cached.Keys)
            {
                PendingDateCache[item] = DateTime.Now;
            }
            var serializer = new SerializerBuilder().Build();
            File.WriteAllText(DateCachePath, serializer.Serialize(PendingDateCache));
        }

        public bool NeedsUpdate(IMusicItem item)
        {
            foreach (var path in RelevantPaths(item))
            {
                var date = TouchedTime(path);
                if (DateCache.TryGetValue(path, out var cached))
                {
                    if (date - TimeSpan.FromSeconds(5) > cached)
                        return true;
                }
                else
                    return true;
            }
            return false;
        }

        private IEnumerable<string> RelevantPaths(IMusicItem item)
        {
            yield return item.Location;
            var art = GetArtPathFor(item);
            if (art != null)
                yield return art;
            foreach (var parent in item.PathFromRoot())
            {
                if (parent.LocalConfig != null)
                    yield return parent.LocalConfig.Location;
            }
        }

        private static DateTime TouchedTime(string filepath)
        {
            DateTime modified = File.GetLastWriteTime(filepath);
            DateTime created = File.GetCreationTime(filepath);
            return modified > created ? modified : created;
        }

        public string GetArtPathFor(IMusicItem item)
        {
            while (item != null)
            {
                var partial = Util.StringPathAfterRoot(item);
                if (item is Song)
                    partial = Path.ChangeExtension(partial, null);
                var path = Path.Combine(Folder, "art", partial + ".png");
                if (File.Exists(path))
                    return path;
                if (item is Song)
                {
                    string parent = Path.GetDirectoryName(partial);
                    string contents = Path.Combine(parent, "__contents__");
                    var contents_path = Path.Combine(Folder, "art", contents + ".png");
                    if (File.Exists(contents_path))
                        return contents_path;
                }
                item = item.Parent;
            }
            return null;
        }

        public void MarkUpdatedRecently(IMusicItem item)
        {
            foreach (var path in RelevantPaths(item))
            {
                PendingDateCache[path] = DateTime.Now;
            }
        }

        public void MarkNeedsUpdateNextTime(IMusicItem item)
        {
            PendingDateCache.Remove(item.Location);
        }

        private SynchedText[] ParseSyncedTexts(string alltext)
        {
            var list = new List<SynchedText>();
            var stamps = Regex.Matches(alltext, @"\[(\d:\d\d:\d\d.\d\d)\]");
            for (int i = 0; i < stamps.Count; i++)
            {
                var time = TimeSpan.ParseExact(stamps[i].Groups[1].Value, "h\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture);
                string text;
                if (i == stamps.Count - 1)
                    text = alltext.Substring(stamps[i].Index + stamps[i].Length);
                else
                    text = alltext.Substring(stamps[i].Index + stamps[i].Length, stamps[i + 1].Index - stamps[i].Index - stamps[i].Length);
                text = text.TrimEnd('\n');
                list.Add(new SynchedText((long)time.TotalMilliseconds, text));
            }
            return list.ToArray();
        }

        // returns true if tag was changed and needs to be resaved
        public bool WriteLyrics(string location, TagLib.Id3v2.Tag tag)
        {
            var lyrics_file = Path.ChangeExtension(Path.Combine(Folder, "lyrics", location), ".lrc");
            // higher priority first
            SynchedText[] frame_lyrics = null;
            SynchedText[] tag_lyrics = null;
            SynchedText[] file_lyrics = null;
            string file_text = null;

            // load lyrics from various sources
            if (File.Exists(lyrics_file))
            {
                file_text = File.ReadAllText(lyrics_file).Replace("\r\n", "\n");
                file_lyrics = ParseSyncedTexts(file_text);
            }
            if (tag != null)
            {
                if (tag.Lyrics != null)
                    tag_lyrics = new SynchedText[] { new SynchedText(0, tag.Lyrics) };
                bool has_frame = false;
                foreach (var frame in tag.GetFrames().ToList())
                {
                    if (frame is SynchronisedLyricsFrame slf)
                    {
                        if (!has_frame)
                        {
                            frame_lyrics = slf.Text;
                            has_frame = true;
                        }
                        else
                        {
                            Logger.WriteLine($"Removed non-initial synced lyrics frame: \"{frame}\"");
                            tag.RemoveFrame(frame);
                        }
                    }
                }
            }

            SynchedText[] chosen_lyrics = frame_lyrics ?? tag_lyrics ?? file_lyrics;
            bool tag_changed = false;

            if (chosen_lyrics != null)
            {
                if (chosen_lyrics != file_lyrics)
                {
                    // write to file
                    var lyrics_text = String.Join("\n", chosen_lyrics.Select(x => $"[{TimeSpan.FromMilliseconds(x.Time):h\\:mm\\:ss\\.ff}]{x.Text}"));
                    if (file_text == null || file_text != lyrics_text)
                    {
                        if (chosen_lyrics == frame_lyrics)
                            Logger.WriteLine($"Wrote lyrics from synced frame to file cache");
                        if (chosen_lyrics == tag_lyrics)
                            Logger.WriteLine($"Wrote lyrics from simple tag to file cache");
                        Directory.CreateDirectory(Path.GetDirectoryName(lyrics_file));
                        File.WriteAllText(lyrics_file, lyrics_text);
                    }
                }

                if (chosen_lyrics != tag_lyrics)
                {
                    // update simple tag
                    var simple_lyrics = String.Join("\n", chosen_lyrics.Select(x => x.Text));
                    if (tag.Lyrics != simple_lyrics)
                    {
                        if (chosen_lyrics == frame_lyrics)
                            Logger.WriteLine($"Wrote lyrics from synced frame to simple tag");
                        if (chosen_lyrics == file_lyrics)
                            Logger.WriteLine($"Wrote lyrics from file cache to simple tag");
                        tag.Lyrics = simple_lyrics;
                        tag_changed = true;
                    }
                }

                if (chosen_lyrics != frame_lyrics)
                {
                    // add frame
                    if (chosen_lyrics == tag_lyrics)
                        Logger.WriteLine($"Wrote lyrics from simple tag to synced frame");
                    if (chosen_lyrics == file_lyrics)
                        Logger.WriteLine($"Wrote lyrics from file cache to synced frame");
                    var frame = new SynchronisedLyricsFrame("lyrics", "english", SynchedTextType.Lyrics, TagLib.StringType.Latin1);
                    frame.Text = chosen_lyrics;
                    tag.AddFrame(frame);
                    tag_changed = true;
                }
            }

            return tag_changed;
        }
    }
}
