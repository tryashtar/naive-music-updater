using System;
using System.Collections.Generic;
using System.Linq;

namespace NaiveMusicUpdater
{
    public class CheckSelectorResults
    {
        public List<IMusicItem> UnselectedItems = new List<IMusicItem>();
        public List<ItemSelector> UnusedSelectors = new List<ItemSelector>();

        public void AddResults(CheckSelectorResults more)
        {
            UnselectedItems.AddRange(more.UnselectedItems);
            UnusedSelectors.AddRange(more.UnusedSelectors);
        }

        public bool AnyUnused => UnselectedItems.Any() || UnusedSelectors.Any();
    }
}
