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
    public class FieldMapMetadataStrategy : IMetadataStrategy
    {
        private readonly Dictionary<MetadataField, IValueResolver> Fields = new();

        public FieldMapMetadataStrategy(YamlMappingNode yaml)
        {
            foreach (var pair in yaml)
            {
                var field = MetadataField.FromID((string)pair.Key);
                if (field != null)
                    Fields[field] = ValueResolverFactory.Create(pair.Value);
            }
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var meta = new Metadata();
            foreach (var pair in Fields)
            {
                if (desired(pair.Key))
                    meta.Register(pair.Key, pair.Value.Resolve(item).ToProperty());
            }
            return meta;
        }
    }
}
