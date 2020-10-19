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
    public struct SongMetadata
    {
        // if HasThing, then it gets used when combining even if null
        // if not HasThing, then it gets skipped
        public readonly string Title;
        public readonly bool HasTitle;
        public readonly string Album;
        public readonly bool HasAlbum;
        public readonly string Artist;
        public readonly bool HasArtist;
        public readonly string Comment;
        public readonly bool HasComment;
        public readonly uint? TrackNumber;
        public readonly bool HasTrackNumber;
        public SongMetadata(string title, string album, string artist, string comment, uint? track_number, bool has_title = true, bool has_album = true, bool has_artist = true, bool has_comment = true, bool has_track_number = true)
        {
            Title = title;
            Album = album;
            Artist = artist;
            Comment = comment;
            HasTitle = has_title;
            HasAlbum = has_album;
            HasArtist = has_artist;
            HasComment = has_comment;
            TrackNumber = track_number;
            HasTrackNumber = has_track_number;
        }

        public SongMetadata Combine(SongMetadata other)
        {
            return new SongMetadata(
                title: other.HasTitle ? other.Title : other.Title ?? Title,
                album: other.HasAlbum ? other.Album : other.Album ?? Album,
                artist: other.HasArtist ? other.Artist : other.Artist ?? Artist,
                comment: other.HasComment ? other.Comment : other.Comment ?? Comment,
                track_number: other.HasTrackNumber ? other.TrackNumber : other.TrackNumber ?? TrackNumber
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

        public abstract string Get(IMusicItem item);

        protected string ResolveNameOrDefault(IMusicItem item, IMusicItem current)
        {
            if (item == current)
                return ConfigReference.CleanName(item.SimpleName);
            return ConfigReference.GetMetadataFor(item).Title;
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

        public override string Get(IMusicItem item)
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

        public override string Get(IMusicItem item)
        {
            var basetext = From.Get(item);
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

        public override string Get(IMusicItem item)
        {
            var text1 = From1.Get(item);
            var text2 = From2.Get(item);
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

        public override string Get(IMusicItem item)
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

        public override string Get(IMusicItem item)
        {
            return Specification;
        }
    }

    public class MetadataStrategy
    {
        private readonly MetadataSelector Title;
        private readonly MetadataSelector Artist;
        private readonly MetadataSelector Album;
        private readonly MetadataSelector Comment;
        public MetadataStrategy(LibraryConfig config, JObject json)
        {
            if (json.TryGetValue("title", out var title))
                Title = MetadataSelector.FromToken(config, title);
            if (json.TryGetValue("artist", out var artist))
                Artist = MetadataSelector.FromToken(config, artist);
            if (json.TryGetValue("album", out var album))
                Album = MetadataSelector.FromToken(config, album);
            if (json.TryGetValue("comment", out var comment))
                Comment = MetadataSelector.FromToken(config, comment);
        }

        public SongMetadata Perform(IMusicItem item)
        {
            // returns null = don't change existing metadata
            // returns <remove> = delete existing metadata
            string title = Title?.Get(item);
            string artist = Artist?.Get(item);
            string album = Album?.Get(item);
            string comment = Comment?.Get(item);
            var track = GetTrackNumber(title);
            if (track != null)
                title = track.Item1;
            uint? track_number = track?.Item2;
            return new SongMetadata(
                title: title == "<remove>" ? null : title,
                artist: artist == "<remove>" ? null : artist,
                album: album == "<remove>" ? null : album,
                comment: comment == "<remove>" ? null : comment,
                track_number: track_number,
                has_title: title != null,
                has_artist: artist != null,
                has_album: album != null,
                has_comment: comment != null,
                has_track_number: track_number != null
            );
        }

        private static readonly Regex TrackNumberRegex = new Regex(@"^(?<number>\d+)\.\s+(?<title>.*)");
        public static Tuple<string, uint> GetTrackNumber(string title)
        {
            if (title == null)
                return null;
            var match = TrackNumberRegex.Match(title);
            if (match.Success)
                return Tuple.Create(match.Groups["title"].Value, uint.Parse(match.Groups["number"].Value));
            return null;
        }
    }
}
