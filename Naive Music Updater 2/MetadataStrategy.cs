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
    public class OptionalProperty<T>
    {
        public readonly T Value;
        public readonly bool IsPresent;
        public OptionalProperty(T item)
        {
            Value = item;
            IsPresent = true;
        }

        public OptionalProperty()
        {
            IsPresent = false;
        }

        public OptionalProperty<T> CombineWith(OptionalProperty<T> other)
        {
            if (other.IsPresent)
                return other;
            return this;
        }

        public OptionalProperty<U> ConvertTo<U>(Func<T, U> converter)
        {
            if (!IsPresent)
                return new OptionalProperty<U>();
            return new OptionalProperty<U>(converter(Value));
        }
    }

    public struct SongMetadata
    {
        public readonly OptionalProperty<string> Title;
        public readonly OptionalProperty<string> Album;
        public readonly OptionalProperty<string> Artist;
        public readonly OptionalProperty<string> Comment;
        public readonly OptionalProperty<uint> TrackNumber;
        public readonly OptionalProperty<string> Language;

        public SongMetadata(
           // these aren't allowed to be null
           OptionalProperty<string> title,
           OptionalProperty<string> album,
           OptionalProperty<string> artist,
           OptionalProperty<string> comment,
           OptionalProperty<uint> track_number,
           OptionalProperty<string> language
        )
        {
            Title = title;
            Album = album;
            Artist = artist;
            Comment = comment;
            TrackNumber = track_number;
            Language = language;
        }

        public SongMetadata Combine(SongMetadata other)
        {
            return new SongMetadata(
                Title.CombineWith(other.Title),
                Album.CombineWith(other.Album),
                Artist.CombineWith(other.Artist),
                Comment.CombineWith(other.Comment),
                TrackNumber.CombineWith(other.TrackNumber),
                Language.CombineWith(other.Language)
            );
        }
    }

    public class SongPredicate
    {
        private readonly string[] Path;
        public SongPredicate(string name)
        {
            Path = name.Split('/');
        }

        public bool Matches(IMusicItem song)
        {
            // only slightly naive
            var songpath = song.PathFromRoot();
            var path1 = String.Join("/", Path) + "/";
            var path2 = String.Join("/", songpath.Select(x => x.SimpleName)) + "/";
            return path2.Contains(path1);
        }

        public override string ToString()
        {
            return String.Join("/", Path);
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
                return new ReplacementsSelector(config, str);
            }
            throw new ArgumentException($"Couldn't figure out what kind of metadata selector this is: {token}");
        }

        public abstract string GetRaw(IMusicItem item);

        public OptionalProperty<string> Get(IMusicItem item)
        {
            string result = GetRaw(item);
            if (result == null)
                return new OptionalProperty<string>();
            if (result == "<remove>")
                return new OptionalProperty<string>(null);
            return new OptionalProperty<string>(result);
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

    public class ReplacementsSelector : MetadataSelector
    {
        string Specification;
        public ReplacementsSelector(LibraryConfig config, string spec) : base(config)
        {
            Specification = spec;
        }

        public override string GetRaw(IMusicItem item)
        {
            return Specification;
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
            return new SongMetadata(
                new OptionalProperty<string>(),
                new OptionalProperty<string>(),
                new OptionalProperty<string>(),
                new OptionalProperty<string>(),
                new OptionalProperty<uint>(),
                new OptionalProperty<string>()
            );
        }
    }

    public class MetadataStrategy : IMetadataStrategy
    {
        private readonly MetadataSelector Title;
        private readonly MetadataSelector Album;
        private readonly MetadataSelector Artist;
        private readonly MetadataSelector Comment;
        private readonly MetadataSelector TrackNumber;
        private readonly MetadataSelector Language;
        public MetadataStrategy(LibraryConfig config, JObject json)
        {
            if (json.TryGetValue("title", out var title))
                Title = MetadataSelector.FromToken(config, title);
            if (json.TryGetValue("album", out var album))
                Album = MetadataSelector.FromToken(config, album);
            if (json.TryGetValue("artist", out var artist))
                Artist = MetadataSelector.FromToken(config, artist);
            if (json.TryGetValue("comment", out var comment))
                Comment = MetadataSelector.FromToken(config, comment);
            if (json.TryGetValue("track", out var track))
                TrackNumber = MetadataSelector.FromToken(config, track);
            if (json.TryGetValue("language", out var lang))
                Language = MetadataSelector.FromToken(config, lang);
        }

        private OptionalProperty<string> Get(MetadataSelector selector, IMusicItem item)
        {
            return selector?.Get(item) ?? new OptionalProperty<string>();
        }

        public SongMetadata Perform(IMusicItem item)
        {
            var title = Get(Title, item);
            var album = Get(Album, item);
            var artist = Get(Artist, item);
            var comment = Get(Comment, item);
            var track = Get(TrackNumber, item).ConvertTo(x => uint.Parse(x));
            var lang = Get(Language, item);
            return new SongMetadata(title, album, artist, comment, track, lang);
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
