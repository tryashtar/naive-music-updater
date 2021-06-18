using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class RegexMatchValue : IValue
    {
        public readonly Match Match;

        public RegexMatchValue(Match match)
        {
            Match = match;
        }

        public MetadataProperty ToProperty()
        {
            throw new InvalidOperationException();
        }

        public string GetGroup(string group)
        {
            return Match.Groups[group].Value;
        }
    }
}
