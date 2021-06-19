using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IMetadataStrategy
    {
        Metadata Get(IMusicItem item, Predicate<MetadataField> desired);
    }

    public static class MetadataStrategyFactory
    {
        public static IMetadataStrategy Create(YamlNode yaml)
        {
            if (yaml is YamlMappingNode map)
            {
                var source = map.Go("source").NullableParse(x => ValueSourceFactory.Create(x));
                if (source != null)
                {
                    var apply = map.Go("apply").Parse(x => FieldSpecFactory.Create(x, true));
                    return new RedirectingMetadataStrategy(source, apply);
                }
                else
                {
                    var apply = map.Parse(x => FieldSpecFactory.Create(x, false));
                    return new DirectMetadataStrategy(apply);
                }
            }
            if (yaml is YamlSequenceNode list)
            {
                var substrats = list.ToList(x => MetadataStrategyFactory.Create(x));
                return new MultipleMetadataStrategy(substrats);
            }
            throw new ArgumentException($"Can't make metadata strategy from {yaml}");
        }
    }
}
