using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib.Flac;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IMetadataStrategy
    {
        Metadata Get(IMusicItem item);
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

        public static IMetadataStrategy Create(YamlNode node)
        {
            if (node is YamlMappingNode map)
                return new MetadataStrategy(map);
            if (node is YamlSequenceNode list)
                return new MultipleMetadataStrategy(list);
            throw new ArgumentException();
        }
    }

    public class NoOpMetadataStrategy : IMetadataStrategy
    {
        public Metadata Get(IMusicItem item)
        {
            return new Metadata();
        }
    }

    public class MetadataStrategy : IMetadataStrategy
    {
        public readonly MetadataSelector Title;
        public readonly MetadataSelector Album;
        public readonly MetadataSelector Artist;
        public readonly MetadataSelector Comment;
        public readonly MetadataSelector TrackNumber;
        public readonly MetadataSelector TrackTotal;
        public readonly MetadataSelector Year;
        public readonly MetadataSelector Language;
        public readonly MetadataSelector Genre;
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

        public MetadataStrategy(YamlMappingNode yaml)
        {
            MetadataSelector FromYaml(string key)
            {
                var item = yaml.TryGet(key);
                if (item == null)
                    return null;
                return MetadataSelectorFactory.FromToken(item);
            }
            Title = FromYaml("title");
            Album = FromYaml("album");
            Artist = FromYaml("artist");
            Comment = FromYaml("comment");
            TrackNumber = FromYaml("track");
            TrackTotal = FromYaml("track_count");
            Year = FromYaml("year");
            Language = FromYaml("language");
            Genre = FromYaml("genre");
        }

        private MetadataProperty<string> Get(MetadataSelector selector, IMusicItem item)
        {
            return selector?.Get(item) ?? MetadataProperty<string>.Ignore();
        }

        public Metadata Get(IMusicItem item)
        {
            var meta = new Metadata()
            {
                Title = Get(Title, item),
                Album = Get(Album, item),
                Artist = Get(Artist, item),
                Comment = Get(Comment, item),
                TrackNumber = Get(TrackNumber, item).TryConvertTo(x => uint.Parse(x)),
                TrackTotal = Get(TrackTotal, item).TryConvertTo(x => uint.Parse(x)),
                Year = Get(Year, item).TryConvertTo(x => uint.Parse(x)),
                Language = Get(Language, item),
                Genre = Get(Genre, item)
            };
            return meta;
        }
    }

    public class ApplyMetadataStrategy : IMetadataStrategy
    {
        public readonly Metadata Data;
        public ApplyMetadataStrategy(Metadata meta)
        {
            Data = meta;
        }

        public Metadata Get(IMusicItem item)
        {
            return Data;
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

        public MultipleMetadataStrategy(YamlSequenceNode yaml)
        {
            Substrategies = new List<IMetadataStrategy>();
            foreach (var item in yaml.Children)
            {
                Substrategies.Add(MetadataStrategyFactory.Create(item));
            }
        }

        public MultipleMetadataStrategy(IEnumerable<IMetadataStrategy> strategies)
        {
            Substrategies = strategies.ToList();
        }

        public Metadata Get(IMusicItem item)
        {
            var datas = Substrategies.Select(x => x.Get(item));
            return Metadata.FromMany(datas);
        }
    }
}
