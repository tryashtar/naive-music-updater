using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class RegexValueOperator : IValueOperator
    {
        public readonly Regex RegexItem;
        public readonly MatchFailDecision MatchFail;

        public RegexValueOperator(YamlMappingNode yaml)
        {
            RegexItem = yaml.Go("regex").Parse(x => new Regex(x.String()));
            MatchFail = yaml.Go("fail").ToEnum(def: MatchFailDecision.Exit);
        }

        public IValue Apply(IValue original)
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
