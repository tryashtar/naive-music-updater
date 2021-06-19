﻿using System;
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
        public static IFieldSetter Create(YamlNode yaml, bool has_context)
        {
            if (yaml is YamlMappingNode map)
            {
                var mode = map.Go("mode").ToEnum(def: CombineMode.Replace);
                if (mode == CombineMode.Remove)
                    return RemoveFieldSetter.Instance;
                var source = map.Go("source").NullableParse(x => ValueSourceFactory.Create(x));
                if (source != null)
                    return new ModeValueSourceFieldSetter(mode, source);
                if (has_context)
                {
                    var modify = map.Go("modify").NullableParse(x => ValueOperatorFactory.Create(x));
                    if (modify != null)
                        return new ModeContextFieldSetter(mode, modify);
                }
            }
            var direct_source = ValueSourceFactory.Create(yaml);
            return new DirectValueSourceFieldSetter(direct_source);
            throw new ArgumentException($"Can't make field setter from {yaml}");
        }
    }
}