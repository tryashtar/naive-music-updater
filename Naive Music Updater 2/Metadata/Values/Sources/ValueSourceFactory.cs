using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IValueSource
    {
        IValue Get(IMusicItem item);
    }

    public static class ValueSourceFactory
    {
        public static IValueSource Create(YamlNode yaml)
        {
            if (yaml is YamlScalarNode scalar)
                return new LiteralStringSource(scalar.Value);
            else if (yaml is YamlSequenceNode sequence)
                return new LiteralListSource(sequence.ToStringList());
            else if (yaml is YamlMappingNode map)
            {
                var selector = map.Go("from").Parse(x => SingleItemSelectorFactory.Create(x));
                var getter = map.Go("value").NullableParse(x => MusicItemGetterFactory.Create(x)) ?? CleanNameGetter.Instance;
                var modifier = map.Go("modify").NullableParse(x => ValueOperatorFactory.Create(x));
                return new MusicItemSource(selector, getter, modifier);
            }
            throw new ArgumentException($"Can't make value resolver from {yaml}");
        }
    }
}
