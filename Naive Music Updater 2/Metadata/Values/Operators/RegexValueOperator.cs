using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class RegexValueOperator : IValueOperator
    {
        public readonly Regex RegexItem;
        public readonly MatchFailDecision MatchFail;

        public RegexValueOperator(Regex regex, MatchFailDecision decision)
        {
            RegexItem = regex;
            MatchFail = decision;
        }

        public IValue Apply(IMusicItem item, IValue original)
        {
            var text = (StringValue)original;

            var match = RegexItem.Match(text.Value);
            if (!match.Success)
                return MatchFail == MatchFailDecision.TakeWhole ? original : MetadataProperty.Ignore();

            return new RegexMatchValue(match);
        }
    }

    public enum MatchFailDecision
    {
        Exit,
        TakeWhole
    }
}
