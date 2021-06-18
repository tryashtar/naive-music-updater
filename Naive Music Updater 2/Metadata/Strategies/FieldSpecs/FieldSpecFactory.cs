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
        public static IFieldSpec Create(YamlNode yaml)
        {
            if (yaml is YamlMappingNode map)
            {
                var type = map.Go("type").ToEnum(def: FieldSpecType.Map);
                if (type == FieldSpecType.Map)
                    return new MapFieldSpec(map);
                else if (type == FieldSpecType.SetAll)
                    return new SetAllFieldSpec(map);
            }
            throw new ArgumentException($"Can't make field spec from {yaml}");
        }
    }

    public enum FieldSpecType
    {
        Map,
        SetAll
    }
}
