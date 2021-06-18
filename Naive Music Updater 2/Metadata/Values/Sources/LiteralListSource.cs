using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class LiteralListSource : IValueSource
    {
        private readonly List<string> Literal;
        public LiteralListSource(IEnumerable<string> literal)
        {
            Literal = literal.ToList();
        }

        public IValue Get(IMusicItem item)
        {
            return new ListValue(Literal);
        }
    }
}
