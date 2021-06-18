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
        public readonly IValueSource Source;
        public readonly IFieldSpec Applier;

        public RedirectingMetadataStrategy(YamlMappingNode yaml)
        {
            Source = yaml.Go("source").Parse(x => ValueSourceFactory.Create(x));
            Applier = yaml.Go("apply").Parse(x => FieldSpecFactory.Create(x));
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var value = Source.Get(item);
            return Applier.ApplyWithContext(item, value, desired);
        }
    }
}
