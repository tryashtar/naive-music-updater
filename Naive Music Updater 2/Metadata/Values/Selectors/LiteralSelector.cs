using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class LiteralSelector : IValueSelector
    {
        public readonly string LiteralText;
        public LiteralSelector(string spec)
        {
            LiteralText = spec;
        }

        public IValue Get(IMusicItem item)
        {
            return new StringValue(LiteralText);
        }
    }
}
