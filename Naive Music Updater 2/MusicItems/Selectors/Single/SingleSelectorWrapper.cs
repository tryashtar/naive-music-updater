using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class SingleSelectorWrapper : ISingleItemSelector
    {
        public readonly IItemSelector Wrapped;
        public SingleSelectorWrapper(IItemSelector wrapped)
        {
            Wrapped=wrapped;
        }

        public IMusicItem SelectFrom(IMusicItem value)
        {
            return Wrapped.AllMatchesFrom(value).SingleOrDefault();
        }
    }
}
