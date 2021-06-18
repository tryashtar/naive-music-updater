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
    public class ValueApplier
    {
        private readonly Dictionary<MetadataField, ValueMetadataConverter> Fields = new();

        public ValueApplier(YamlNode yaml)
        {
            Fields = yaml.ToDictionary(
                x => MetadataField.FromID(x.String()),
                x => new ValueMetadataConverter(x)
            );
        }

        public Metadata Apply(IValue value, Predicate<MetadataField> desired)
        {
            var meta = new Metadata();
            foreach (var pair in Fields)
            {
                if (desired(pair.Key))
                    meta.Register(pair.Key, pair.Value.Convert(value));
            }
            return meta;
        }
    }
}
