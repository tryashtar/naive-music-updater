using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IItemSelector
    {
        IEnumerable<IMusicItem> AllMatchesFrom(IMusicItem start);
        bool IsSelectedFrom(IMusicItem start, IMusicItem item);
        IEnumerable<IItemSelector> UnusedFrom(IMusicItem start);
    }
}
