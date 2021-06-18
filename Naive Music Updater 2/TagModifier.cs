using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using File = System.IO.File;
using System.Text.RegularExpressions;
using TagLib.Id3v2;

namespace NaiveMusicUpdater
{
    public class TagModifier
    {
        public bool HasChanged { get; private set; }
        private readonly TagLib.File TagFile;
        private readonly LibraryCache Cache;
        public TagModifier(TagLib.File file, LibraryCache cache)
        {
            TagFile = file;
            Cache = cache;
        }

        public void UpdateMetadata(Metadata metadata)
        {
            var interop = TagInteropFactory.GetDynamicInterop(TagFile.Tag);
            foreach (var field in MetadataField.Values)
            {
                interop.Set(field, metadata.Get(field));
            }
            interop.WipeUselessProperties();
            if (interop.Changed)
                HasChanged = true;
        }

        public void WriteLyrics(string location)
        {
            var lyrics_file = Path.ChangeExtension(Path.Combine(Cache.Folder, "lyrics", location), ".lrc");

            // higher priority first
            SynchedText[] frame_lyrics = null;
            SynchedText[] file_lyrics = null;
            SynchedText[] tag_lyrics = null;
            string[] file_text = null;

            var id3v2 = (TagLib.Id3v2.Tag)TagFile.GetTag(TagTypes.Id3v2);
            var ogg = (TagLib.Ogg.XiphComment)TagFile.GetTag(TagTypes.Xiph);

            // load lyrics from cached file
            if (File.Exists(lyrics_file))
            {
                file_text = File.ReadAllLines(lyrics_file);
                file_lyrics = ParseSyncedTexts(file_text);
            }

            // load simple lyrics from tag
            if (TagFile.Tag.Lyrics != null)
                tag_lyrics = TagFile.Tag.Lyrics.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(x => new SynchedText(0, x)).ToArray();

            // load synced lyrics from id3v2 tag
            if (id3v2 != null)
            {
                foreach (var frame in id3v2.GetFrames<SynchronisedLyricsFrame>())
                {
                    frame_lyrics = frame.Text;
                    break;
                }
            }
            else
            {
                if (ogg != null)
                {
                    var lyrics = ogg.GetField("SYNCED LYRICS");
                    if (lyrics != null && lyrics.Length > 0)
                        frame_lyrics = ParseSyncedTexts(lyrics);
                }
            }

            SynchedText[] chosen_lyrics = frame_lyrics ?? file_lyrics ?? tag_lyrics;

            if (chosen_lyrics != null)
            {
                if (chosen_lyrics != file_lyrics)
                {
                    // write to file
                    var lyrics_text = SerializeSyncedTexts(chosen_lyrics);
                    if (file_text == null || !file_text.SequenceEqual(lyrics_text))
                    {
                        if (chosen_lyrics == frame_lyrics)
                            Logger.WriteLine($"Wrote lyrics from synced frame to file cache");
                        if (chosen_lyrics == tag_lyrics)
                            Logger.WriteLine($"Wrote lyrics from simple tag to file cache");
                        Directory.CreateDirectory(Path.GetDirectoryName(lyrics_file));
                        File.WriteAllLines(lyrics_file, lyrics_text);
                    }
                }

                if (chosen_lyrics != tag_lyrics)
                {
                    // update simple tag
                    var simple_lyrics = String.Join("\n", chosen_lyrics.Select(x => x.Text));
                    if (TagFile.Tag.Lyrics != simple_lyrics)
                    {
                        if (chosen_lyrics == frame_lyrics)
                            Logger.WriteLine($"Wrote lyrics from synced frame to simple tag");
                        if (chosen_lyrics == file_lyrics)
                            Logger.WriteLine($"Wrote lyrics from file cache to simple tag");
                        Logger.WriteLine($"Old lyrics:");
                        Logger.WriteLine(TagFile.Tag.Lyrics);
                        TagFile.Tag.Lyrics = simple_lyrics;
                        HasChanged = true;
                    }
                }

                if (chosen_lyrics != frame_lyrics)
                {
                    if (id3v2 != null)
                    {
                        // add frame
                        if (chosen_lyrics == tag_lyrics)
                            Logger.WriteLine($"Wrote lyrics from simple tag to synced frame");
                        if (chosen_lyrics == file_lyrics)
                            Logger.WriteLine($"Wrote lyrics from file cache to synced frame");
                        foreach (var frame in id3v2.GetFrames<SynchronisedLyricsFrame>().ToList())
                        {
                            id3v2.RemoveFrame(frame);
                        }
                        var lyrics = new SynchronisedLyricsFrame("lyrics", Id3v2TagInterop.GetLanguage(id3v2), SynchedTextType.Lyrics, StringType.Latin1);
                        lyrics.Text = chosen_lyrics;
                        id3v2.AddFrame(lyrics);
                        HasChanged = true;
                    }
                    if (ogg != null)
                    {
                        if (chosen_lyrics == tag_lyrics)
                            Logger.WriteLine($"Wrote lyrics from simple tag to synced flac");
                        if (chosen_lyrics == file_lyrics)
                            Logger.WriteLine($"Wrote lyrics from file cache to synced flac");
                        ogg.SetField("SYNCED LYRICS", SerializeSyncedTexts(chosen_lyrics));
                        HasChanged = true;
                    }
                }
            }
        }

        public void UpdateArt(string art_path)
        {
            var picture = art_path == null ? null : ArtCache.GetPicture(art_path);
            if (!IsSingleValue(TagFile.Tag.Pictures, picture))
            {
                if (picture == null)
                {
                    if (TagFile.Tag.Pictures.Length == 0)
                        return;
                    Logger.WriteLine($"Deleted art");
                    TagFile.Tag.Pictures = new IPicture[0];
                }
                else
                {
                    Logger.WriteLine($"Added art");
                    TagFile.Tag.Pictures = new IPicture[] { picture };
                }
                HasChanged = true;
            }
        }

        private static readonly string[] TimespanFormats = new string[] { @"h\:mm\:ss\.FFF", @"mm\:ss\.FFF", @"m\:ss\.FFF", @"h\:mm\:ss", @"mm\:ss", @"m\:ss" };
        private static SynchedText[] ParseSyncedTexts(string[] lines)
        {
            var list = new List<SynchedText>();
            var regex = new Regex(@"\[(?<time>.+)\](?<line>.+)");
            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    if (TimeSpan.TryParseExact(match.Groups["time"].Value, TimespanFormats, null, out var time))
                        list.Add(new SynchedText((long)time.TotalMilliseconds, match.Groups["line"].Value));
                }
            }
            return list.ToArray();
        }

        private static string[] SerializeSyncedTexts(SynchedText[] lines)
        {
            return lines.Select(x => $"[{StringTimeSpan(TimeSpan.FromMilliseconds(x.Time))}]{x.Text}").ToArray();
        }

        private static string StringTimeSpan(TimeSpan time)
        {
            if (time.TotalHours < 1)
                return time.ToString(@"mm\:ss\.ff");
            return time.ToString(@"h\:mm\:ss\.ff");
        }

        private static bool IsSingleValue(IPicture[] array, IPicture value)
        {
            if (array == null)
                return false;
            if (value == null)
                return array.Length == 0;
            if (array.Length != 1)
                return false;

            return value.Data == array[0].Data;
        }
    }
}
