using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class ParentItemSelector : ISingleItemSelector
    {
        public readonly int Up;
        public ParentItemSelector(int up)
        {
            Up = up;
        }

        public IMusicItem SelectFrom(IMusicItem value)
        {
            for (int i = 0; i < Up; i++)
            {
                if (value.Parent == null)
                    return null;
                value = value.Parent;
            }
            return value;
        }
    }
}
