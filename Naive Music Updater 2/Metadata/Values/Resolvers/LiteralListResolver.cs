using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class LiteralListResolver : IValueResolver
    {
        private readonly List<string> Literal;
        public LiteralListResolver(IEnumerable<string> literal)
        {
            Literal = literal.ToList();
        }

        public IValue Resolve(IMusicItem item)
        {
            return new ListValue(Literal);
        }
    }
}
