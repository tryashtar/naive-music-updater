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
    public interface IMetadataStrategy
    {
        SongMetadata Perform(IMusicItem item);
    }

    public static class MetadataStrategyFactory
    {
        public static IMetadataStrategy Create(JToken token)
        {
            if (token is JObject obj)
                return new MetadataStrategy(obj);
            else if (token is JArray arr)
                return new MultipleMetadataStrategy(arr);
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
        public MetadataStrategy(JObject json)
        {
            MetadataSelector FromJson(string key)
            {
                if (json.TryGetValue(key, out var item))
                    return MetadataSelectorFactory.FromToken(item);
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
        public MultipleMetadataStrategy(JArray json)
        {
            Substrategies = new List<IMetadataStrategy>();
            foreach (var item in json)
            {
                Substrategies.Add(MetadataStrategyFactory.Create(item));
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
