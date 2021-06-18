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
    public class SetAllFieldSpec : IFieldSpec
    {
        private readonly HashSet<MetadataField> Fields;
        public readonly IFieldSetter Setter;

        public SetAllFieldSpec(YamlMappingNode yaml)
        {
            var fields = yaml.Go("fields");
            if (fields is YamlScalarNode scalar && scalar.Value == "*")
                Fields = MetadataField.Values.ToHashSet();
            else
                Fields = fields.ToList(x => MetadataField.FromID(x.String()))?.ToHashSet();
            Setter = yaml.Go("set").Parse(x => FieldSetterFactory.Create(x));
        }

        private Metadata ApplyLike(Predicate<MetadataField> desired, Func<IFieldSetter, MetadataProperty> get)
        {
            var meta = new Metadata();
            foreach (var field in Fields)
            {
                if (desired(field))
                    meta.Register(field, get(Setter));
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
