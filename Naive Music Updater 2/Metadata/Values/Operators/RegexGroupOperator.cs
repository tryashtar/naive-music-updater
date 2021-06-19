using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class RegexGroupOperator : IValueOperator
    {
        public readonly string Group;

        public RegexGroupOperator(string group)
        {
            Group = group;
        }

        public IValue Apply(IMusicItem item, IValue original)
        {
            var text = (RegexMatchValue)original;
            return new StringValue(text.GetGroup(Group));
        }
    }
}
