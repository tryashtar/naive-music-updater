using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class LiteralStringResolver : IValueResolver
    {
        public readonly string Literal;
        public LiteralStringResolver(string literal)
        {
            Literal = literal;
        }

        public IValue Resolve(IMusicItem item)
        {
            return new StringValue(Literal);
        }
    }
}
