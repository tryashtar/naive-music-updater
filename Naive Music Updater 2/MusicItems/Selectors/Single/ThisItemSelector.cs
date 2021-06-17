using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class ThisItemSelector : ISingleItemSelector
    {
        public static readonly ThisItemSelector Instance = new();

        public IMusicItem SelectFrom(IMusicItem start)
        {
            return start;
        }
    }
}
