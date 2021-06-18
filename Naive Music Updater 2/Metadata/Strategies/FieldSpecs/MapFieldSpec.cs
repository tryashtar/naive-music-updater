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
    public class MapFieldSpec : IFieldSpec
    {
        private readonly Dictionary<MetadataField, IFieldSetter> Fields;

        public MapFieldSpec(YamlNode yaml)
        {
            Fields = yaml.ToDictionary(
                x => MetadataField.FromID(x.String()),
                x => FieldSetterFactory.Create(x)
            );
        }

        private Metadata ApplyLike(Predicate<MetadataField> desired, Func<IFieldSetter, MetadataProperty> get)
        {
            var meta = new Metadata();
            foreach (var pair in Fields)
            {
                if (desired(pair.Key))
                    meta.Register(pair.Key, get(pair.Value));
            }
            return meta;
        }

        public Metadata Apply(IMusicItem item, Predicate<MetadataField> desired)
        {
            return ApplyLike(desired, x => x.Get(item));
        }

        public Metadata ApplyWithContext(IMusicItem item, IValue value, Predicate<MetadataField> desired)
        {
            return ApplyLike(desired, x => x.GetWithContext(item, value));
        }
    }
}
