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
    public class SourcedResolver : IValueResolver
    {
        public readonly ISingleItemSelector Selector;
        public readonly IValueSelector Getter;
        public readonly IValueOperator Modifier;
        public SourcedResolver(YamlMappingNode node)
        {
            Selector = SingleItemSelectorFactory.Create(node["from"]);
            Getter = ValueSelectorFactory.Create(node["value"]);
            Modifier = node.ParseOrDefault("modify", x => ValueOperatorFactory.Create(x));
        }

        public IValue Resolve(IMusicItem item)
        {
            item = Selector.SelectFrom(item);
            var value = Getter.Get(item);
            if (Modifier != null)
                value = Modifier.Apply(value);
            return value;
        }
    }
}
