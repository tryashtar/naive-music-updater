using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib.Flac;

namespace NaiveMusicUpdater
{
    public class MetadataProperty<T>
    {
        public readonly T Value;
        public readonly bool Overwrite;

        private MetadataProperty(T item, bool overwrite)
        {
            Value = item;
            Overwrite = overwrite;
        }

        public static MetadataProperty<T> Create(T item)
        {
            return new MetadataProperty<T>(item, true);
        }

        public static MetadataProperty<T> Delete()
        {
            return new MetadataProperty<T>(default, true);
        }

        public static MetadataProperty<T> Ignore()
        {
            return new MetadataProperty<T>(default, false);
        }

        public MetadataProperty<T> CombineWith(MetadataProperty<T> other)
        {
            if (other.Overwrite)
                return other;
            return this;
        }

        public MetadataProperty<U> ConvertTo<U>(Func<T, U> converter)
        {
            if (!Overwrite)
                return MetadataProperty<U>.Ignore();
            return new MetadataProperty<U>(converter(Value), Overwrite);
        }
    }

    public class SongMetadataBuilder
    {
        public MetadataProperty<string> Title;
        public MetadataProperty<string> Album;
        public MetadataProperty<string> Artist;
        public MetadataProperty<string> Comment;
        public MetadataProperty<uint> TrackNumber;
        public MetadataProperty<uint> TrackTotal;
        public MetadataProperty<uint> Year;
        public MetadataProperty<string> Language;
        public MetadataProperty<string> Genre;
        public SongMetadataBuilder()
        { }

        public SongMetadata Build()
        {
            return new SongMetadata(
                Title ?? MetadataProperty<string>.Ignore(),
                Album ?? MetadataProperty<string>.Ignore(),
                Artist ?? MetadataProperty<string>.Ignore(),
                Comment ?? MetadataProperty<string>.Ignore(),
                TrackNumber ?? MetadataProperty<uint>.Ignore(),
                TrackTotal ?? MetadataProperty<uint>.Ignore(),
                Year ?? MetadataProperty<uint>.Ignore(),
                Language ?? MetadataProperty<string>.Ignore(),
                Genre ?? MetadataProperty<string>.Ignore()
            );
        }
    }

    public class SongMetadata
    {
        public readonly MetadataProperty<string> Title;
        public readonly MetadataProperty<string> Album;
        public readonly MetadataProperty<string> Artist;
        public readonly MetadataProperty<string> Comment;
        public readonly MetadataProperty<uint> TrackNumber;
        public readonly MetadataProperty<uint> TrackTotal;
        public readonly MetadataProperty<uint> Year;
        public readonly MetadataProperty<string> Language;
        public readonly MetadataProperty<string> Genre;

        public SongMetadata(
           // these aren't allowed to be null
           MetadataProperty<string> title,
           MetadataProperty<string> album,
           MetadataProperty<string> artist,
           MetadataProperty<string> comment,
           MetadataProperty<uint> track_number,
           MetadataProperty<uint> track_total,
           MetadataProperty<uint> year,
           MetadataProperty<string> language,
           MetadataProperty<string> genre
        )
        {
            Title = title;
            Album = album;
            Artist = artist;
            Comment = comment;
            TrackNumber = track_number;
            TrackTotal = track_total;
            Year = year;
            Language = language;
            Genre = genre;
        }

        public SongMetadata Combine(SongMetadata other)
        {
            return new SongMetadata(
                Title.CombineWith(other.Title),
                Album.CombineWith(other.Album),
                Artist.CombineWith(other.Artist),
                Comment.CombineWith(other.Comment),
                TrackNumber.CombineWith(other.TrackNumber),
                TrackTotal.CombineWith(other.TrackTotal),
                Year.CombineWith(other.Year),
                Language.CombineWith(other.Language),
                Genre.CombineWith(other.Genre)
            );
        }

        public static SongMetadata Merge(IEnumerable<SongMetadata> metas)
        {
            return metas.Aggregate((x, y) => x.Combine(y));
        }
    }

    public abstract class MetadataSelector
    {
        protected LibraryConfig ConfigReference;
        public MetadataSelector(LibraryConfig config)
        {
            ConfigReference = config;
        }
        public static MetadataSelector FromToken(LibraryConfig config, JToken token)
        {
            if (token.Type == JTokenType.Integer)
                return new SimpleParentSelector(config, (int)token);
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                if (obj.TryGetValue("operation", out var operation))
                {
                    if ((string)operation == "split")
                        return new SplitOperationSelector(config, obj);
                    else if ((string)operation == "join")
                        return new JoinOperationSelector(config, obj);
                    else if ((string)operation == "regex")
                        return new RegexSelector(config, obj);
                }
            }
            if (token.Type == JTokenType.String)
            {
                string str = (string)token;
                if (str == "<this>")
                    return new FilenameSelector(config);
                if (str == "<title>")
                    return new GetMetadataSelector(config, x => x.Title);
                return new LiteralSelector(config, str);
            }
            throw new ArgumentException($"Couldn't figure out what kind of metadata selector this is: {token}");
        }

        public abstract string GetRaw(IMusicItem item);

        public MetadataProperty<string> Get(IMusicItem item)
        {
            string result = GetRaw(item);
            if (result == null)
                return MetadataProperty<string>.Ignore();
            if (result == "<remove>")
                return MetadataProperty<string>.Delete();
            return MetadataProperty<string>.Create(result);
        }

        protected string ResolveNameOrDefault(IMusicItem item, IMusicItem current)
        {
            if (item == current)
                return ConfigReference.CleanName(item.SimpleName);
            return ConfigReference.GetMetadataFor(item).Title.Value;
        }
    }

    // cannot be used to get itself, use "<this>" instead
    public class SimpleParentSelector : MetadataSelector
    {
        private readonly int Number;
        public SimpleParentSelector(LibraryConfig config, int number) : base(config)
        {
            Number = number;
        }

        public override string GetRaw(IMusicItem item)
        {
            IMusicItem found;
            var list = item.PathFromRoot().ToList();
            if (Number >= 0)
            {
                if (Number >= list.Count)
                    return null;
                found = list[Number];
            }
            else
            {
                int index = list.Count + Number - 1;
                if (index < 0)
                    return null;
                found = list[index];
            }
            if (found == item)
                return null;
            return ResolveNameOrDefault(found, item);
        }
    }

    public class SplitOperationSelector : MetadataSelector
    {
        private readonly MetadataSelector From;
        private readonly string Separator;
        private readonly int Index;
        private readonly NoSeparatorDecision NoSeparator;
        private readonly OutofBoundsDecision OutofBounds;

        private enum NoSeparatorDecision
        {
            Exit,
            Ignore
        }

        private enum OutofBoundsDecision
        {
            Exit,
            Wrap,
            Clamp
        }

        // gets metadata "From" somewhere else and extracts a part of it by splitting the string and taking one of its pieces
        public SplitOperationSelector(LibraryConfig config, JObject data) : base(config)
        {
            From = MetadataSelector.FromToken(config, data["from"]);
            Separator = (string)data["separator"];
            Index = (int)data["index"];
            NoSeparator = NoSeparatorDecision.Ignore;
            if (data.TryGetValue("no_separator", out var sep) && (string)sep == "exit")
                NoSeparator = NoSeparatorDecision.Exit;
            OutofBounds = OutofBoundsDecision.Exit;
            if (data.TryGetValue("out_of_bounds", out var bounds))
            {
                if ((string)bounds == "wrap")
                    OutofBounds = OutofBoundsDecision.Wrap;
                if ((string)bounds == "clamp")
                    OutofBounds = OutofBoundsDecision.Clamp;
            }
        }

        public override string GetRaw(IMusicItem item)
        {
            var basetext = From.GetRaw(item);
            if (basetext == null)
                return null;
            string[] parts = basetext.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && NoSeparator == NoSeparatorDecision.Exit)
                return null;
            int index = Index;
            if (index < 0 || index >= parts.Length)
            {
                if (OutofBounds == OutofBoundsDecision.Exit)
                    return null;
                if (OutofBounds == OutofBoundsDecision.Wrap)
                    index %= parts.Length;
                if (OutofBounds == OutofBoundsDecision.Clamp)
                    index = Math.Max(0, Math.Min(parts.Length - 1, index));
            }
            return parts[index];
        }
    }

    public class RegexSelector : MetadataSelector
    {
        private readonly MetadataSelector From;
        private readonly Regex Regex;
        private readonly string Group;
        private readonly MatchFailDecision MatchFail;

        private enum MatchFailDecision
        {
            Exit,
            Ignore
        }

        // gets metadata "From" somewhere else and extracts a part of it by splitting the string and taking one of its pieces
        public RegexSelector(LibraryConfig config, JObject data) : base(config)
        {
            From = MetadataSelector.FromToken(config, data["from"]);
            Regex = new Regex((string)data["regex"]);
            Group = (string)data["group"];
            MatchFail = MatchFailDecision.Ignore;
            if (data.TryGetValue("fail", out var fail) && (string)fail == "exit")
                MatchFail = MatchFailDecision.Exit;
        }

        public override string GetRaw(IMusicItem item)
        {
            var basetext = From.GetRaw(item);
            if (basetext == null)
                return null;
            var match = Regex.Match(basetext);
            if (!match.Success)
                return MatchFail == MatchFailDecision.Ignore ? basetext : null;
            return match.Groups[Group].Value;
        }
    }

    public class JoinOperationSelector : MetadataSelector
    {
        private readonly MetadataSelector From1;
        private readonly MetadataSelector From2;
        private readonly string With;

        // gets metadata "From" two other places and combines them with "With" in between
        public JoinOperationSelector(LibraryConfig config, JObject data) : base(config)
        {
            From1 = MetadataSelector.FromToken(config, data["from1"]);
            From2 = MetadataSelector.FromToken(config, data["from2"]);
            With = (string)data["with"];
        }

        public override string GetRaw(IMusicItem item)
        {
            var text1 = From1.GetRaw(item);
            var text2 = From2.GetRaw(item);
            if (text1 == null && text2 == null)
                return null;
            if (text1 == null)
                return text2;
            if (text2 == null)
                return text1;
            return text1 + With + text2;
        }
    }

    public class FilenameSelector : MetadataSelector
    {
        public FilenameSelector(LibraryConfig config) : base(config)
        { }

        public override string GetRaw(IMusicItem item)
        {
            return ResolveNameOrDefault(item, item);
        }
    }

    public class LiteralSelector : MetadataSelector
    {
        private readonly string LiteralText;
        public LiteralSelector(LibraryConfig config, string spec) : base(config)
        {
            LiteralText = spec;
        }

        public override string GetRaw(IMusicItem item)
        {
            return LiteralText;
        }
    }

    public class GetMetadataSelector : MetadataSelector
    {
        public delegate MetadataProperty<string> MetadataGetter(SongMetadata meta);
        private readonly MetadataGetter Getter;
        public GetMetadataSelector(LibraryConfig config, MetadataGetter getter) : base(config)
        {
            Getter = getter;
        }

        public override string GetRaw(IMusicItem item)
        {
            return Getter(item.GetMetadata()).Value;
        }
    }

    public interface IMetadataStrategy
    {
        SongMetadata Perform(IMusicItem item);
    }

    public static class MetadataStrategyFactory
    {
        public static IMetadataStrategy Create(LibraryConfig config, JToken token)
        {
            if (token is JObject obj)
                return new MetadataStrategy(config, obj);
            else if (token is JArray arr)
                return new MultipleMetadataStrategy(config, arr);
            throw new ArgumentException();
        }
    }

    public class NoOpMetadataStrategy : IMetadataStrategy
    {
        public SongMetadata Perform(IMusicItem item)
        {
            return new SongMetadataBuilder().Build();
        }
    }

    public class MetadataStrategy : IMetadataStrategy
    {
        private readonly MetadataSelector Title;
        private readonly MetadataSelector Album;
        private readonly MetadataSelector Artist;
        private readonly MetadataSelector Comment;
        private readonly MetadataSelector TrackNumber;
        private readonly MetadataSelector TrackTotal;
        private readonly MetadataSelector Year;
        private readonly MetadataSelector Language;
        private readonly MetadataSelector Genre;
        public MetadataStrategy(LibraryConfig config, JObject json)
        {
            MetadataSelector FromJson(string key)
            {
                if (json.TryGetValue(key, out var item))
                    return MetadataSelector.FromToken(config, item);
                return null;
            }
            Title = FromJson("title");
            Album = FromJson("album");
            Artist = FromJson("artist");
            Comment = FromJson("comment");
            TrackNumber = FromJson("track");
            TrackTotal = FromJson("track_count");
            Year = FromJson("year");
            Language = FromJson("language");
            Genre = FromJson("genre");
        }

        private MetadataProperty<string> Get(MetadataSelector selector, IMusicItem item)
        {
            return selector?.Get(item) ?? MetadataProperty<string>.Ignore();
        }

        public SongMetadata Perform(IMusicItem item)
        {
            var title = Get(Title, item);
            var album = Get(Album, item);
            var artist = Get(Artist, item);
            var comment = Get(Comment, item);
            var track = Get(TrackNumber, item).ConvertTo(x => uint.Parse(x));
            var track_total = Get(TrackTotal, item).ConvertTo(x => uint.Parse(x));
            var year = Get(Year, item).ConvertTo(x => uint.Parse(x));
            var lang = Get(Language, item);
            var genre = Get(Genre, item);
            return new SongMetadata(title, album, artist, comment, track, track_total, year, lang, genre);
        }
    }

    public class MultipleMetadataStrategy : IMetadataStrategy
    {
        private readonly List<IMetadataStrategy> Substrategies;
        public MultipleMetadataStrategy(LibraryConfig config, JArray json)
        {
            Substrategies = new List<IMetadataStrategy>();
            foreach (var item in json)
            {
                Substrategies.Add(MetadataStrategyFactory.Create(config, item));
            }
        }

        public MultipleMetadataStrategy(IEnumerable<IMetadataStrategy> strategies)
        {
            Substrategies = strategies.ToList();
        }

        public SongMetadata Perform(IMusicItem item)
        {
            var metadata = Substrategies.First().Perform(item);
            foreach (var strategy in Substrategies.Skip(1))
            {
                var extra = strategy.Perform(item);
                metadata = metadata.Combine(extra);
            }
            return metadata;
        }
    }
}
