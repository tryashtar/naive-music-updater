using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib.Flac;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class LiteralResolver : IValueResolver
    {
        public readonly string Literal;
        public LiteralResolver(string literal)
        {
            Literal = literal;
        }

        public IValue Resolve(IMusicItem item)
        {
            return new StringValue(Literal);
        }
    }
}
