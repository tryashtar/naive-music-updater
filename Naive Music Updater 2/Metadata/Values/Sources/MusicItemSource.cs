using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class MusicItemSource : IValueSource
    {
        public readonly ISingleItemSelector Selector;
        public readonly IMusicItemValueSource Getter;
        public readonly IValueOperator Modifier;

        public MusicItemSource(ISingleItemSelector selector, IMusicItemValueSource getter, IValueOperator modifier)
        {
            Selector = selector;
            Getter = getter;
            Modifier = modifier;
        }

        public IValue Get(IMusicItem item)
        {
            item = Selector.SelectFrom(item);
            if (item == null)
                return BlankValue.Instance;
            var value = Getter.Get(item);
            if (Modifier != null)
                value = Modifier.Apply(item, value);
            return value;
        }
    }
}
