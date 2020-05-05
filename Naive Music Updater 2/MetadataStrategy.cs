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
        public readonly string Title;
        public readonly string Album;
        public readonly string Artist;
        public readonly string Comment;
        public SongMetadata(string title, string album, string artist, string comment)
        {
            Title = title;
            Album = album;
            Artist = artist;
            Comment = comment;
        }

        public SongMetadata Combine(SongMetadata other)
        {
            return new SongMetadata(
                title: other.Title ?? Title,
                album: other.Album ?? Album,
                artist: other.Artist ?? Artist,
                comment: other.Comment ?? Comment
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
                if (obj.TryGetValue("from", out var from) &&
                    obj.TryGetValue("operation", out var operation) &&
                    (string)operation == "split" &&
                    obj.TryGetValue("separator", out var separator) &&
                    obj.TryGetValue("index", out var index)
                )
                    return new SplitOperationSelector(config, from, (string)separator, (int)index);
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
            var list = item.PathFromRoot().ToList();
            if (Number >= 0)
            {
                if (Number >= list.Count)
                    return null;
                return ResolveNameOrDefault(list[Number], item);
            }
            int index = list.Count - Number;
            if (index < 0)
                return null;
            return ResolveNameOrDefault(list[Number], item);
        }
    }

    public class SplitOperationSelector : MetadataSelector
    {
        private readonly MetadataSelector From;
        private readonly string Separator;
        private readonly int Index;
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
            return new SongMetadata(
                title: Title?.Get(item),
                artist: Artist?.Get(item),
                album: Album?.Get(item),
                comment: Comment?.Get(item)
            );
        }
    }
}
