using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public SongMetadata(string title, string album, string artist, string comment, bool has_title = true, bool has_album = true, bool has_artist = true, bool has_comment = true)
        {
            Title = title;
            Album = album;
            Artist = artist;
            Comment = comment;
            HasTitle = has_title;
            HasAlbum = has_album;
            HasArtist = has_artist;
            HasComment = has_comment;
        }

        public SongMetadata Combine(SongMetadata other)
        {
            return new SongMetadata(
                title: other.HasTitle ? other.Title : other.Title ?? Title,
                album: other.HasAlbum ? other.Album : other.Album ?? Album,
                artist: other.HasArtist ? other.Artist : other.Artist ?? Artist,
                comment: other.HasComment ? other.Comment : other.Comment ?? Comment
            );
        }
    }

    public class SongPredicate
    {
        private string[] Path;
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
                    {
                        if (obj.TryGetValue("from", out var from) &&
                            obj.TryGetValue("separator", out var separator) &&
                            obj.TryGetValue("index", out var index))
                            return new SplitOperationSelector(config, from, (string)separator, (int)index);
                    }
                    else if ((string)operation == "join")
                    {
                        if (obj.TryGetValue("from1", out var from1) &&
                            obj.TryGetValue("from2", out var from2) &&
                            obj.TryGetValue("with", out var with))
                            return new JoinOperationSelector(config, from1, from2, (string)with);
                    }
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
            int index = list.Count - Number;
            if (index < 0)
                return null;
            found = list[Number];
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

        // gets metadata "From" somewhere else and extracts a part of it by splitting the string and taking one of its pieces
        public SplitOperationSelector(LibraryConfig config, JToken from, string separator, int index) : base(config)
        {
            From = MetadataSelector.FromToken(config, from);
            Separator = separator;
            Index = index;
        }

        public override string Get(IMusicItem item)
        {
            var basetext = From.Get(item);
            if (basetext == null)
                return null;
            string[] parts = basetext.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (Index < 0 || Index >= parts.Length)
                return null;
            return parts[Index];
        }
    }

    public class JoinOperationSelector : MetadataSelector
    {
        private readonly MetadataSelector From1;
        private readonly MetadataSelector From2;
        private readonly string With;

        // gets metadata "From" two other places and combines them with "With" in between
        public JoinOperationSelector(LibraryConfig config, JToken from1, JToken from2, string with) : base(config)
        {
            From1 = MetadataSelector.FromToken(config, from1);
            From2 = MetadataSelector.FromToken(config, from2);
            With = with;
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
            var title = Title?.Get(item);
            var artist = Artist?.Get(item);
            var album = Album?.Get(item);
            var comment = Comment?.Get(item);
            return new SongMetadata(
                title: title == "<remove>" ? null : title,
                artist: artist == "<remove>" ? null : artist,
                album: album == "<remove>" ? null : album,
                comment: comment == "<remove>" ? null : comment,
                has_title: title != null,
                has_artist: artist != null,
                has_album: album != null,
                has_comment: comment != null
            );
        }
    }
}
