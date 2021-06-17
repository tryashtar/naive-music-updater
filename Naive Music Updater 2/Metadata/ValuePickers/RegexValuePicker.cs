using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class RegexValuePicker : IValuePicker
    {
        public readonly Regex RegexItem;
        public readonly string Replace;
        public readonly MatchFailDecision MatchFail;

        public RegexValuePicker(YamlMappingNode yaml)
        {
            RegexItem = new Regex((string)yaml["regex"]);
            Replace = (string)yaml["replace"];
            MatchFail = yaml.ParseOrDefault("fail", x => Util.ParseUnderscoredEnum<MatchFailDecision>((string)x), MatchFailDecision.Exit);
        }

        public MetadataProperty PickFrom(MetadataProperty full)
        {
            var basetext = full.Value;
            var match = RegexItem.Match(basetext);
            if (!match.Success)
                return MatchFail == MatchFailDecision.TakeWhole ? full : MetadataProperty.Ignore();
            return MetadataProperty.Single(RegexItem.Replace(basetext, Replace), full.CombineMode);
        }
    }

    public enum MatchFailDecision
    {
        Exit,
        TakeWhole
    }
}
