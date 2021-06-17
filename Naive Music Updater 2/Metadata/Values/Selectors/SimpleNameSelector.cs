using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class SimpleNameSelector : IValueSelector
    {
        public static readonly SimpleNameSelector Instance = new();

        public IValue Get(IMusicItem item)
        {
            return new StringValue(item.SimpleName);
        }
    }
}
