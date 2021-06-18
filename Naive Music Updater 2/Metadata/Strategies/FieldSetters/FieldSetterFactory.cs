using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IFieldSetter
    {
        MetadataProperty Get(IMusicItem item);
        MetadataProperty GetWithContext(IMusicItem item, IValue value);
    }

    public static class FieldSetterFactory
    {
        public static IFieldSetter Create(YamlNode yaml)
        {
            if (yaml is YamlMappingNode map)
            {
                var type = map.Go("type").ToEnum(def: FieldSetterType.DirectValue);
                if (type == FieldSetterType.DirectValue)
                    return new DirectValueSourceFieldSetter(map);
                else if (type == FieldSetterType.Value)
                    return new ModeValueSourceFieldSetter(map);
                else if (type == FieldSetterType.Context)
                    return new ModeContextFieldSetter(map);
            }
            else
                return new DirectValueSourceFieldSetter(yaml);
            throw new ArgumentException($"Can't make field setter from {yaml}");
        }
    }

    public enum FieldSetterType
    {
        DirectValue,
        Value,
        Context
    }
}
