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
    public class RedirectingMetadataStrategy : IMetadataStrategy
    {
        public readonly SelectorDirector Director;

        public RedirectingMetadataStrategy(YamlMappingNode yaml)
        {
            Director = new SelectorDirector((YamlMappingNode)yaml["strat"]);
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            return Director.Get(item, desired);
        }
    }

    public class SelectorDirector
    {
        public readonly MetadataSelector Selector;
        public readonly Dictionary<MetadataField, IValuePicker> Fields = new();

        public SelectorDirector(YamlMappingNode yaml)
        {
            Selector = MetadataSelectorFactory.Create(yaml["selector"]);
            foreach (var pair in (YamlMappingNode)yaml["assign"])
            {
                var field = MetadataField.FromID((string)pair.Key);
                if (field != null)
                    Fields[field] = ValuePickerFactory.Create(pair.Value);
            }
        }

        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            var meta = new Metadata();
            foreach (var pair in Fields)
            {
                if (desired(pair.Key))
                {
                    var base_value = Selector.Get(item);
                    meta.Register(pair.Key, pair.Value.PickFrom(base_value));
                }
            }
            return meta;
        }
    }
}
