using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class SimpleNameGetter : IMusicItemValueSource
    {
        public static readonly SimpleNameGetter Instance = new();

        public IValue Get(IMusicItem item)
        {
            return new StringValue(item.SimpleName);
        }
    }
}
