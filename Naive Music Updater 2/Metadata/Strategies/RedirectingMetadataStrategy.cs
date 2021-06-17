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
    public class RedirectingMetadataStrategy : IMetadataStrategy
    {
        public readonly IValueResolver Resolver;
        public readonly ValueApplier Applier;

        public RedirectingMetadataStrategy(YamlMappingNode yaml)
        {
            Resolver = ValueResolverFactory.Create((YamlMappingNode)yaml["take"]);
            Applier = new ValueApplier((YamlMappingNode)yaml["apply"]);
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var value = Resolver.Resolve(item);
            return Applier.Apply(value, desired);
        }
    }

    public class ValueApplier
    {
        private readonly Dictionary<MetadataField, IValueOperator> Fields = new();

        public ValueApplier(YamlMappingNode yaml)
        {
            foreach (var pair in yaml)
            {
                var field = MetadataField.FromID((string)pair.Key);
                if (field != null)
                    Fields[field] = ValueOperatorFactory.Create(pair.Value);
            }
        }

        public Metadata Apply(IValue value, Predicate<MetadataField> desired)
        {
            var meta = new Metadata();
            foreach (var pair in Fields)
            {
                if (desired(pair.Key))
                    meta.Register(pair.Key, pair.Value.Apply(value).ToProperty());
            }
            return meta;
        }
    }
}
