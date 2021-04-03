using System;
using System.Collections.Generic;

namespace NaiveMusicUpdater
{
    public class CheckSelectorResults
    {
        public List<IMusicItem> UnselectedItems = new List<IMusicItem>();
        public List<ItemSelector> UnusedSelectors = new List<ItemSelector>();
    }
}
