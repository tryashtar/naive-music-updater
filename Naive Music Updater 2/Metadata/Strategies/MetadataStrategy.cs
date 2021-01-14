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
        Metadata Get(IMusicItem item, Predicate<MetadataField> desired);
    }

    public class MetadataStrategy : IMetadataStrategy
    {
        private readonly Dictionary<MetadataField, MetadataSelector> Fields = new Dictionary<MetadataField, MetadataSelector>();
        
        public MetadataStrategy(YamlMappingNode yaml)
        {
            foreach (var pair in yaml)
            {
                var field = MetadataField.FromID((string)pair.Key);
                if (field != null)
                    Fields[field] = MetadataSelectorFactory.FromToken(pair.Value);
            }
        }

        private MetadataProperty Get(MetadataSelector selector, IMusicItem item)
        {
            return selector?.Get(item) ?? MetadataProperty.Ignore();
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var meta = new Metadata();
            foreach (var pair in Fields)
            {
                if (desired(pair.Key))
                    meta.Register(pair.Key, Get(pair.Value, item));
            }
            return meta;
        }
    }
}
