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
    public interface ITagInterop
    {
        MetadataProperty Get(MetadataField field);
        void Set(MetadataField field, MetadataProperty value);
        void WipeUselessProperties();
        bool Changed { get; }
    }

    public static class TagInteropFactory
    {
        public static ITagInterop GetDynamicInterop(dynamic tag)
        {
            return GetInterop(tag);
        }

        private static ITagInterop GetInterop(TagLib.Id3v2.Tag tag) => new Id3v2TagInterop(tag);
        private static ITagInterop GetInterop(TagLib.Id3v1.Tag tag) => new Id3v1TagInterop(tag);
        private static ITagInterop GetInterop(TagLib.Ape.Tag tag) => new ApeTagInterop(tag);
        private static ITagInterop GetInterop(TagLib.Ogg.XiphComment tag) => new XiphTagInterop(tag);
        private static ITagInterop GetInterop(CombinedTag tag) => new MultipleInterop(tag);
    }

    public class MultipleInterop : ITagInterop
    {
        private readonly List<ITagInterop> Interops;
        public bool Changed => Interops.Any(x => x.Changed);
        public MultipleInterop(CombinedTag tag)
        {
            Interops = tag.Tags.Select(x => TagInteropFactory.GetDynamicInterop(x)).ToList();
        }

        public MetadataProperty Get(MetadataField field)
        {
            foreach (var interop in Interops)
            {
                var result = interop.Get(field);
                if (result.Value != null)
                    return result;
            }
            return MetadataProperty.Ignore();
        }

        public void Set(MetadataField field, MetadataProperty value)
        {
            foreach (var interop in Interops)
            {
                interop.Set(field, value);
            }
        }

        public void WipeUselessProperties()
        {
            foreach (var interop in Interops)
            {
                interop.WipeUselessProperties();
            }
        }
    }

    public delegate MetadataProperty Getter();
    public delegate void Setter(MetadataProperty value);
    public delegate bool Equal(MetadataProperty p1, MetadataProperty p2);

    public class InteropDelegates
    {
        public readonly Getter Getter;
        public readonly Setter Setter;
        public readonly Equal Equal;
        public InteropDelegates(Getter getter, Setter setter, Equal equal)
        {
            Getter = getter;
            Setter = setter;
            Equal = equal;
        }
    }

    public delegate WipeResult Wiper();
    public class WipeDelegates
    {
        public readonly Wiper Wipe;
        public WipeDelegates(Wiper wipe)
        {
            Wipe = wipe;
        }
    }

    public record WipeResult
    {
        public string OldValue { get; init; }
        public string NewValue { get; init; }
        public bool Changed { get; init; }
    }

    public abstract class AbstractInterop<T> : ITagInterop where T : Tag
    {
        protected T Tag;
        private readonly TagTypes TagType;
        public bool Changed { get; protected set; } = false;
        private readonly Dictionary<MetadataField, InteropDelegates> Schema;
        private readonly Dictionary<string, WipeDelegates> WipeSchema;

        public AbstractInterop(T tag)
        {
            Tag = tag;
            TagType = tag.TagTypes;
            Schema = CreateSchema();
            WipeSchema = CreateWipeSchema();
        }

        protected abstract Dictionary<MetadataField, InteropDelegates> CreateSchema();
        protected abstract Dictionary<string, WipeDelegates> CreateWipeSchema();

        public MetadataProperty Get(MetadataField field)
        {
            if (Schema.TryGetValue(field, out var entry))
                return entry.Getter();
            return MetadataProperty.Ignore();
        }

        public void Set(MetadataField field, MetadataProperty value)
        {
            if (Schema.TryGetValue(field, out var entry))
                Replace(field, entry, value);
        }

        private void Replace(MetadataField field, InteropDelegates delegates, MetadataProperty incoming)
        {
            var current = delegates.Getter();
            var result = MetadataProperty.Combine(current, incoming);
            if (!delegates.Equal(current, result))
            {
                Logger.WriteLine($"Changing {field.Name} in {TagType} tag from \"{current}\" to \"{result}\"");
                delegates.Setter(result);
                Changed = true;
            }
        }

        public void WipeUselessProperties()
        {
            foreach (var item in WipeSchema)
            {
                var result = item.Value.Wipe();
                if (result.Changed)
                {
                    Logger.WriteLine($"Wiped {item.Key} in {TagType} tag from \"{result.OldValue}\" to \"{result.NewValue}\"");
                    Changed = true;
                }
            }
        }

        protected static MetadataProperty Get(string str)
        {
            return MetadataProperty.Single(str, CombineMode.Replace);
        }

        protected static MetadataProperty Get(uint num)
        {
            return MetadataProperty.Single(num.ToString(), CombineMode.Replace);
        }

        protected static MetadataProperty Get(string[] str)
        {
            return MetadataProperty.List(str.ToList(), CombineMode.Replace);
        }

        protected static string[] Array(MetadataProperty prop)
        {
            return prop.ListValue.ToArray();
        }

        protected static uint Number(MetadataProperty prop)
        {
            if (prop.Value == null)
                return 0;
            return uint.Parse(prop.Value);
        }

        protected static bool StringEqual(MetadataProperty p1, MetadataProperty p2)
        {
            if (String.IsNullOrEmpty(p1.Value) && String.IsNullOrEmpty(p2.Value))
                return true;
            return p1.ListValue.SequenceEqual(p2.ListValue);
        }

        protected static bool NumberEqual(MetadataProperty p1, MetadataProperty p2)
        {
            var n1 = p1.ListValue.Select(uint.Parse);
            var n2 = p2.ListValue.Select(uint.Parse);
            if (IsZero(n1) && IsZero(n2))
                return true;
            return n1.SequenceEqual(n2);
        }

        private static bool IsZero(IEnumerable<uint> sequence)
        {
            if (!sequence.Any())
                return true;
            if (sequence.Count() == 1 && sequence.Single() == 0)
                return true;
            return false;
        }

        protected static InteropDelegates Delegates(Getter get, Setter set)
        {
            return new InteropDelegates(get, set, StringEqual);
        }

        protected static InteropDelegates NumDelegates(Getter get, Setter set)
        {
            return new InteropDelegates(get, set, NumberEqual);
        }

        protected static WipeDelegates SimpleWipeRet(Func<string> get, Func<bool> set)
        {
            return new WipeDelegates(() =>
            {
                var before = get();
                bool changed = set();
                var after = get();
                return new WipeResult()
                {
                    OldValue = before,
                    NewValue = after,
                    Changed = changed
                };
            });
        }

        protected static WipeDelegates SimpleWipe(Func<string> get, Action set)
        {
            return SimpleWipeRet(get, () =>
            {
                var before = get();
                set();
                var after = get();
                return before != after;
            });
        }

        protected static WipeDelegates SimpleWipe(Func<uint> get, Action set)
        {
            return SimpleWipeRet(() => get().ToString(), () =>
            {
                var before = get();
                set();
                var after = get();
                return before != after;
            });
        }

        protected static WipeDelegates SimpleWipe(Func<string[]> get, Action set)
        {
            return SimpleWipeRet(() => get().ToString(), () =>
            {
                var before = get();
                set();
                var after = get();
                return !ArrayEquals(before, after);
            });
        }

        private static bool ArrayEquals<U>(U[] one, U[] two)
        {
            if (one == null)
                return two == null;
            if (two == null)
                return one == null;
            return one.SequenceEqual(two);
        }
    }

    public class BasicInterop : AbstractInterop<Tag>
    {
        public BasicInterop(Tag tag) : base(tag) { }
        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            return BasicSchema(Tag);
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            return BasicWipeSchema(Tag);
        }

        public static Dictionary<MetadataField, InteropDelegates> BasicSchema(Tag tag)
        {
            return new Dictionary<MetadataField, InteropDelegates>
            {
                { MetadataField.Album, Delegates(() => Get(tag.Album), x => tag.Album = x.Value) },
                { MetadataField.AlbumArtists, Delegates(() => Get(tag.AlbumArtists), x => tag.AlbumArtists = Array(x)) },
                { MetadataField.Arranger, Delegates(() => Get(tag.RemixedBy), x => tag.RemixedBy = x.Value) },
                { MetadataField.Comment, Delegates(() => Get(tag.Comment), x => tag.Comment = x.Value) },
                { MetadataField.Composers, Delegates(() => Get(tag.Composers), x => tag.Composers = Array(x)) },
                { MetadataField.Genres, Delegates(() => Get(tag.Genres), x => tag.Genres = Array(x)) },
                { MetadataField.Performers, Delegates(() => Get(tag.Performers), x => tag.Performers = Array(x)) },
                { MetadataField.Title, Delegates(() => Get(tag.Title), x => tag.Title = x.Value) },
                { MetadataField.Track, NumDelegates(() => Get(tag.Track), x => tag.Track = Number(x)) },
                { MetadataField.TrackTotal, NumDelegates(() => Get(tag.TrackCount), x => tag.TrackCount = Number(x)) },
                { MetadataField.Year, NumDelegates(() => Get(tag.Year), x => tag.Year = Number(x)) },
            };
        }

        public static Dictionary<string, WipeDelegates> BasicWipeSchema(Tag tag)
        {
            return new Dictionary<string, WipeDelegates>
            {
                { "publisher", SimpleWipe(() => tag.Publisher, () => tag.Publisher = null) },
                { "bpm", SimpleWipe(() => tag.BeatsPerMinute, () => tag.BeatsPerMinute = 0) },
                { "description", SimpleWipe(() => tag.Description, () => tag.Description = null) },
                { "grouping", SimpleWipe(() => tag.Grouping, () => tag.Grouping = null) },
                { "subtitle", SimpleWipe(() => tag.Subtitle, () => tag.Subtitle = null) },
                { "amazon id", SimpleWipe(() => tag.AmazonId, () => tag.AmazonId = null) },
                { "conductor", SimpleWipe(() => tag.Conductor, () => tag.Conductor = null) },
                { "copyright", SimpleWipe(() => tag.Copyright, () => tag.Copyright = null) },
                { "disc number", SimpleWipe(() => tag.Disc, () => tag.Disc = 0) },
                { "disc count", SimpleWipe(() => tag.DiscCount, () => tag.DiscCount = 0) },
                { "musicbrainz data", SimpleWipe(() => GetMusicBrainz(tag), () => WipeMusicBrainz(tag)) },
                { "music ip", SimpleWipe(() => tag.MusicIpId, () => tag.MusicIpId = null) },
            };
        }

        private static string[] GetMusicBrainz(Tag tag)
        {
            return new string[] {
                tag.MusicBrainzArtistId,
                tag.MusicBrainzDiscId,
                tag.MusicBrainzReleaseArtistId,
                tag.MusicBrainzReleaseCountry,
                tag.MusicBrainzReleaseId,
                tag.MusicBrainzReleaseStatus,
                tag.MusicBrainzReleaseType,
                tag.MusicBrainzTrackId,
            };
        }

        private static void WipeMusicBrainz(Tag tag)
        {
            tag.MusicBrainzArtistId = null;
            tag.MusicBrainzDiscId = null;
            tag.MusicBrainzReleaseArtistId = null;
            tag.MusicBrainzReleaseCountry = null;
            tag.MusicBrainzReleaseId = null;
            tag.MusicBrainzReleaseStatus = null;
            tag.MusicBrainzReleaseType = null;
            tag.MusicBrainzTrackId = null;
        }
    }

    public class Id3v2TagInterop : AbstractInterop<TagLib.Id3v2.Tag>
    {
        private static readonly string[] ReadDelimiters = new string[] { "/", "; ", ";" };
        private const string WriteDelimiter = "; ";
        public Id3v2TagInterop(TagLib.Id3v2.Tag tag) : base(tag)
        {
            tag.ReadArtistDelimiters = ReadDelimiters;
            tag.WriteArtistDelimiter = WriteDelimiter;
        }

        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            var schema = BasicInterop.BasicSchema(Tag);
            schema[MetadataField.Language] = Delegates(() => Get(GetLanguage(Tag)), x => SetLanguage(Tag, x.Value));
            return schema;
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            var schema = BasicInterop.BasicWipeSchema(Tag);
            schema.Add("compilation", SimpleWipeRet(() => Tag.IsCompilation.ToString(), () => Tag.IsCompilation = false));
            AddFrameWipes(schema);
            return schema;
        }

        private void AddFrameWipes(Dictionary<string, WipeDelegates> schema)
        {

        }

        private const string LANGUAGE_TAG = "TLAN";
        public static string GetLanguage(TagLib.Id3v2.Tag tag)
        {
            foreach (var frame in tag.GetFrames<TextInformationFrame>().ToList())
            {
                if (frame.FrameId.ToString() == LANGUAGE_TAG)
                {
                    if (frame.Text.Length > 0)
                        return frame.Text.First();
                }
            }
            return null;
        }

        public static void SetLanguage(TagLib.Id3v2.Tag tag, string value)
        {
            foreach (var frame in tag.GetFrames<TextInformationFrame>().ToList())
            {
                if (frame.FrameId.ToString() == LANGUAGE_TAG)
                    tag.RemoveFrame(frame);
            }
            var lang = new TextInformationFrame(ByteVector.FromString(LANGUAGE_TAG, StringType.UTF8));
            lang.Text = new[] { value };
            tag.AddFrame(lang);
        }
    }

    public class Id3v1TagInterop : AbstractInterop<TagLib.Id3v1.Tag>
    {
        public Id3v1TagInterop(TagLib.Id3v1.Tag tag) : base(tag) { }
        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            var schema = BasicInterop.BasicSchema(Tag);
            schema.Remove(MetadataField.AlbumArtists);
            schema.Remove(MetadataField.Composers);
            schema.Remove(MetadataField.Arranger);
            schema.Remove(MetadataField.TrackTotal);
            void SetPrimitive(MetadataField field, int length)
            {
                var existing = schema[field];
                schema[field] = new InteropDelegates(existing.Getter, existing.Setter, (a, b) => PrimitiveEqual(a, b, length));
            }
            SetPrimitive(MetadataField.Title, 30);
            SetPrimitive(MetadataField.Performers, 30);
            SetPrimitive(MetadataField.Album, 30);
            SetPrimitive(MetadataField.Comment, 28);
            return schema;
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            var schema = BasicInterop.BasicWipeSchema(Tag);
            return schema;
        }

        private bool PrimitiveEqual(MetadataProperty p1, MetadataProperty p2, int length)
        {
            var value1 = PrimitiveIfy(p1.Value, length);
            var value2 = PrimitiveIfy(p2.Value, length);
            return value1 == value2;
        }

        private string PrimitiveIfy(string value, int length)
        {
            return TagLib.Id3v1.Tag.DefaultStringHandler.Render(value).Resize(length).ToString().Trim().TrimEnd('\0');
        }
    }

    public class ApeTagInterop : AbstractInterop<TagLib.Ape.Tag>
    {
        public ApeTagInterop(TagLib.Ape.Tag tag) : base(tag) { }
        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            var schema = BasicInterop.BasicSchema(Tag);
            schema.Remove(MetadataField.Arranger);
            return schema;
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            var schema = BasicInterop.BasicWipeSchema(Tag);
            return schema;
        }
    }

    public class XiphTagInterop : AbstractInterop<TagLib.Ogg.XiphComment>
    {
        public XiphTagInterop(TagLib.Ogg.XiphComment tag) : base(tag) { }
        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            var schema = BasicInterop.BasicSchema(Tag);
            return schema;
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            var schema = BasicInterop.BasicWipeSchema(Tag);
            AddFieldWipes(schema, "LABEL", "ISRC", "BARCODE");
            return schema;
        }

        private void AddFieldWipes(Dictionary<string, WipeDelegates> schema, params string[] fields)
        {
            foreach (var field in fields)
            {
                schema.Add(field, SimpleWipe(() => String.Join(";", Tag.GetField(field)), () => Tag.RemoveField(field)));
            }
        }
    }
}
