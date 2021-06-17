using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class CleanNameSelector : IValueSelector
    {
        public static readonly CleanNameSelector Instance = new();

        public IValue Get(IMusicItem item)
        {
            return new StringValue(item.GlobalCache.Config.CleanName(item.SimpleName));
        }
    }
}
