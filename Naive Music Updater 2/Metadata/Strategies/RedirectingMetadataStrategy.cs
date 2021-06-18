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
            Resolver = yaml.Go("take").Parse(x => ValueResolverFactory.Create(x));
            Applier = yaml.Go("apply").Parse(x => new ValueApplier(x));
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var value = Resolver.Resolve(item);
            return Applier.Apply(value, desired);
        }
    }
}
