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
    public class DirectMetadataStrategy : IMetadataStrategy
    {
        public readonly IFieldSpec Applier;

        public DirectMetadataStrategy(YamlMappingNode yaml)
        {
            Applier = FieldSpecFactory.Create(yaml);
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            return Applier.Apply(item, desired);
        }
    }
}
