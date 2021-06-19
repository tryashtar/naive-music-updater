using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IFieldSpec
    {
        Metadata Apply(IMusicItem item, Predicate<MetadataField> desired);
        Metadata ApplyWithContext(IMusicItem item, IValue value, Predicate<MetadataField> desired);
    }

    public static class FieldSpecFactory
    {
        public static IFieldSpec Create(YamlNode yaml, bool has_context)
        {
            if (yaml is YamlMappingNode map)
            {
                var fields = yaml.Go("fields");
                if (fields != null)
                {
                    IEnumerable<MetadataField> set;
                    if (fields is YamlScalarNode scalar && scalar.Value == "*")
                        set = MetadataField.Values;
                    else
                        set = fields.ToList(x => MetadataField.FromID(x.String()));
                    var setter = yaml.Go("set").Parse(x => FieldSetterFactory.Create(x, has_context));
                    return new SetAllFieldSpec(set.ToHashSet(), setter);
                }
                else
                {
                    var direct = yaml.ToDictionary(
                        x => MetadataField.FromID(x.String()),
                        x => FieldSetterFactory.Create(x, has_context)
                    );
                    return new MapFieldSpec(direct);
                }
            }
            throw new ArgumentException($"Can't make field spec from {yaml}");
        }
    }
}
