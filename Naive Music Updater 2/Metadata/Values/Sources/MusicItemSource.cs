using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class MusicItemSource : IValueSource
    {
        public readonly ISingleItemSelector Selector;
        public readonly IMusicItemValueSource Getter;
        public readonly IValueOperator Modifier;
        public MusicItemSource(YamlMappingNode node)
        {
            Selector = node.Go("from").Parse(x => SingleItemSelectorFactory.Create(x));
            Getter = node.Go("value").Parse(x => MusicItemGetterFactory.Create(x));
            Modifier = node.Go("modify").NullableParse(x => ValueOperatorFactory.Create(x));
        }

        public IValue Get(IMusicItem item)
        {
            item = Selector.SelectFrom(item);
            var value = Getter.Get(item);
            if (Modifier != null)
                value = Modifier.Apply(value);
            return value;
        }
    }
}
