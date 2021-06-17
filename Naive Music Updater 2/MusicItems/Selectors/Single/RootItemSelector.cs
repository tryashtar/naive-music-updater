using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class RootItemSelector : ISingleItemSelector
    {
        public readonly int Down;
        public RootItemSelector(int down)
        {
            Down = down;
        }

        public IMusicItem SelectFrom(IMusicItem value)
        {
            var path = value.PathFromRoot().ToList();
            if (Down >= path.Count)
                return null;
            return path[Down];
        }
    }
}
