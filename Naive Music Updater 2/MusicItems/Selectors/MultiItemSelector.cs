using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class MultiItemSelector : IItemSelector
    {
        private readonly IItemSelector[] Subselectors;
        public MultiItemSelector(YamlSequenceNode sequence)
        {
            Subselectors = sequence.Children.Select(x => ItemSelectorFactory.FromNode(x)).ToArray();
        }

        public IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start)
        {
            foreach (var item in Subselectors)
            {
                var submatches = item.AllMatchesFrom(start);
                foreach (var sub in submatches) { yield return sub; }
            }
        }

        public bool IsSelectedFrom(IMusicItem start, IMusicItem item)
        {
            return Subselectors.Any(x => x.IsSelectedFrom(start, item));
        }

        public IEnumerable<IItemSelector> UnusedFrom(IMusicItem start)
        {
            return Subselectors.SelectMany(x => x.UnusedFrom(start));
        }
    }
}
