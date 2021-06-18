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
                var mode = map.Go("mode").ToEnum<CombineMode>();
                if (mode == null)
                {
                    var source = ValueSourceFactory.Create(map);
                    return new DirectValueSourceFieldSetter(source);
                }
                else
                {
                    var source = map.Go("source").NullableParse(x => ValueSourceFactory.Create(x));
                    if (source != null)
                        return new ModeValueSourceFieldSetter(mode.Value, source);
                    else
                    {
                        var modify = map.Go("modify").NullableParse(x => ValueOperatorFactory.Create(x));
                        return new ModeContextFieldSetter(mode.Value, modify);
                    }
                }
            }
            throw new ArgumentException($"Can't make field setter from {yaml}");
        }
    }
}
