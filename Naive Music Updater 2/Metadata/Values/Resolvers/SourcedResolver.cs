using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            Selector = node.Go("from").Parse(x => SingleItemSelectorFactory.Create(x));
            Getter = node.Go("value").Parse(x => ValueSelectorFactory.Create(x));
            Modifier = node.Go("modify").Parse(x => ValueOperatorFactory.Create(x));
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
