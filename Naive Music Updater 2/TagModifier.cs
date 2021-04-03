using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using YamlDotNet.RepresentationModel;
using TagLib;
using File = System.IO.File;
using Tag = TagLib.Tag;
using System.Text.RegularExpressions;
using System.Globalization;
using TagLib.Id3v2;
using Microsoft.CSharp.RuntimeBinder;

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
            if (interop.Changed)
                HasChanged = true;
        }

        public void WipeUselessProperties()
        {
            InvokeDynamicDo(x => WipeUselessPropertiesDynamic(x));
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
                    var lyrics_text = chosen_lyrics.Select(x => $"[{TimeSpan.FromMilliseconds(x.Time):h\\:mm\\:ss\\.ff}]{x.Text}").ToArray();
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

        private void InvokeDynamicDo(Action<dynamic> function)
        {
            void wrapped_function(Tag t)
            {
                try
                {
                    function(t);
                }
                catch (RuntimeBinderException)
                {
                    Logger.WriteLine($"Unsure how to handle {t.GetType()} for {function.Method.Name}");
                }
            }
            DynamicDo((dynamic)TagFile.Tag, (Action<Tag>)wrapped_function);
        }

        private void DynamicDo(Tag tag, Action<Tag> function)
        {
            function(tag);
        }

        private void DynamicDo(CombinedTag tag, Action<Tag> function)
        {
            foreach (var sub in tag.Tags)
            {
                DynamicDo((dynamic)sub, function);
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
            return lines.Select(x => $"[{StringTimeSpan(TimeSpan.FromMilliseconds(x.Time), 3)}]{x.Text}").ToArray();
        }

        private static string StringTimeSpan(TimeSpan time, int decimals = 0)
        {
            // create decimal part if requested
            string ending = decimals > 0 ? time.ToString(new string('F', decimals)) : "";
            // only include the dot if there will be digits after it
            if (ending != "")
                ending = "." + ending;
            if (time.TotalMinutes < 10)
                return time.ToString(@"m\:ss") + ending;
            if (time.TotalHours < 1)
                return time.ToString(@"mm\:ss") + ending;
            return time.ToString(@"h\:mm\:ss") + ending;
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

        private void Remove(string thing, object data)
        {
            string rep = data.ToString();
            if (data is IEnumerable<string> arr)
                rep = String.Join(";", arr);
            Logger.WriteLine($"Removing {thing} (was \"{rep}\")");
        }

        #region WipeUselessPropertiesDynamic
        private void WipeUselessPropertiesDynamic(Tag tag)
        {
            if (tag.Publisher != null)
            {
                Remove("publisher", tag.Publisher);
                tag.Publisher = null;
                HasChanged = true;
            }
            if (tag.BeatsPerMinute != 0)
            {
                Remove("beats per minute", tag.BeatsPerMinute);
                tag.BeatsPerMinute = 0;
                HasChanged = true;
            }
            if (tag.Description != null)
            {
                Remove("description", tag.Description);
                tag.Description = null;
                HasChanged = true;
            }
            if (tag.Grouping != null)
            {
                Remove("grouping", tag.Grouping);
                tag.Grouping = null;
                HasChanged = true;
            }
            if (tag.Subtitle != null)
            {
                Remove("subtitle", tag.Subtitle);
                tag.Subtitle = null;
                HasChanged = true;
            }
            if (tag.AmazonId != null)
            {
                Remove("amazon id", tag.AmazonId);
                tag.AmazonId = null;
                HasChanged = true;
            }
            if (tag.Conductor != null)
            {
                Remove("conductor", tag.Conductor);
                tag.Conductor = null;
                HasChanged = true;
            }
            if (tag.Copyright != null)
            {
                Remove("copyright", tag.Copyright);
                tag.Copyright = null;
                HasChanged = true;
            }
            if (tag.Disc != 0)
            {
                Remove("disc number", tag.Disc);
                tag.Disc = 0;
                HasChanged = true;
            }
            if (tag.DiscCount != 0)
            {
                Remove("disc count", tag.DiscCount);
                tag.DiscCount = 0;
                HasChanged = true;
            }
            if (tag.MusicBrainzArtistId != null ||
                tag.MusicBrainzDiscId != null ||
                tag.MusicBrainzReleaseArtistId != null ||
                tag.MusicBrainzReleaseCountry != null ||
                tag.MusicBrainzReleaseId != null ||
                tag.MusicBrainzReleaseStatus != null ||
                tag.MusicBrainzReleaseType != null ||
                tag.MusicBrainzTrackId != null)
            {
                Remove("musicbrainz data", new string[] { tag.MusicBrainzArtistId,
                tag.MusicBrainzDiscId,
                tag.MusicBrainzReleaseArtistId,
                tag.MusicBrainzReleaseCountry,
                tag.MusicBrainzReleaseId,
                tag.MusicBrainzReleaseStatus,
                tag.MusicBrainzReleaseType,
                tag.MusicBrainzTrackId, });

                tag.MusicBrainzArtistId = null;
                tag.MusicBrainzDiscId = null;
                tag.MusicBrainzReleaseArtistId = null;
                tag.MusicBrainzReleaseCountry = null;
                tag.MusicBrainzReleaseId = null;
                tag.MusicBrainzReleaseStatus = null;
                tag.MusicBrainzReleaseType = null;
                tag.MusicBrainzTrackId = null;
                HasChanged = true;
            }
            if (tag.MusicIpId != null)
            {
                Remove("music IP", tag.MusicIpId);
                tag.MusicIpId = null;
                HasChanged = true;
            }
        }

        private void WipeUselessPropertiesDynamic(TagLib.Ogg.XiphComment tag)
        {
            WipeUselessPropertiesDynamic((Tag)tag);
            var label = tag.GetField("LABEL");
            if (label != null && label.Length > 0)
            {
                Remove("LABEL", label);
                tag.RemoveField("LABEL");
                HasChanged = true;
            }
            var isrc = tag.GetField("ISRC");
            if (isrc != null && isrc.Length > 0)
            {
                Remove("ISRC", isrc);
                tag.RemoveField("ISRC");
                HasChanged = true;
            }
            var bar = tag.GetField("BARCODE");
            if (bar != null && bar.Length > 0)
            {
                Remove("BARCODE", bar);
                tag.RemoveField("BARCODE");
                HasChanged = true;
            }
        }

        private void WipeUselessPropertiesDynamic(TagLib.Id3v2.Tag tag)
        {
            WipeUselessPropertiesDynamic((Tag)tag);
            if (tag.IsCompilation != false)
            {
                Remove("compilation", tag.IsCompilation);
                tag.IsCompilation = false;
                HasChanged = true;
            }
            foreach (var frame in tag.GetFrames().ToList())
            {
                bool remove = false;
                if (frame is TextInformationFrame tif)
                {
                    //Logger.WriteLine(String.Join("\n",tif.Text));
                    if (tif.Text.Length == 0)
                    {
                        Logger.WriteLine($"Removed text information frame with length {tif.Text.Length}: \"{tif}\"");
                        remove = true;
                    }
                    else
                    {
                        if (!Cache.Config.ShouldKeepFrame(tif))
                        {
                            Logger.WriteLine($"Removed text information frame of type {tif.FrameId} not carrying tag data: \"{tif}\"");
                            remove = true;
                        }
                    }
                }
                else if (frame is CommentsFrame cf)
                {
                    if (cf.Text != tag.Comment)
                    {
                        Logger.WriteLine($"Removed comment frame not matching comment: \"{cf}\"");
                        remove = true;
                    }
                }
                else if (frame is UnsynchronisedLyricsFrame ulf)
                {
                    if (ulf.Text != tag.Lyrics)
                    {
                        Logger.WriteLine($"Removed lyrics frame not matching lyrics: \"{ulf}\"");
                        remove = true;
                    }
                }
                else if (frame is MusicCdIdentifierFrame mcd)
                {
                    Logger.WriteLine($"Removed music CD identifier frame: \"{mcd.Data}\"");
                    remove = true;
                }
                else if (frame is UrlLinkFrame urlf)
                {
                    Logger.WriteLine($"Removed URL link frame: \"{urlf}\"");
                    remove = true;
                }
                else if (frame is UnknownFrame unkf)
                {
                    Logger.WriteLine($"Removed unknown frame: \"{unkf.Data}\"");
                    remove = true;
                }
                else if (frame is PrivateFrame pf)
                {
                    if (Cache.Config.IsIllegalPrivateOwner(pf.Owner))
                    {
                        Logger.WriteLine($"Removed private frame with owner \"{pf.Owner}\": \"{pf.PrivateData}\"");
                        remove = true;
                    }
                }
                if (remove)
                {
                    tag.RemoveFrame(frame);
                    HasChanged = true;
                }
            }
        }
        #endregion
    }
}
