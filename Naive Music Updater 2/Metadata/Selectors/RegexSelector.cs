using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class RegexSelector : MetadataSelector
    {
        private readonly MetadataSelector From;
        private readonly Regex Regex;
        private readonly string Group;
        private readonly MatchFailDecision MatchFail;

        private enum MatchFailDecision
        {
            Exit,
            Ignore
        }

        // gets metadata "From" somewhere else and extracts a part of it by splitting the string and taking one of its pieces
        public RegexSelector(YamlMappingNode yaml)
        {
            From = MetadataSelectorFactory.FromToken(yaml["from"]);
            Regex = new Regex((string)yaml["regex"]);
            Group = (string)yaml["group"];
            MatchFail = MatchFailDecision.Ignore;
            var fail = (string)yaml.TryGet("fail");
            if (fail == "exit")
                MatchFail = MatchFailDecision.Exit;
        }

        public override string GetRaw(IMusicItem item)
        {
            var basetext = From.GetRaw(item);
            if (basetext == null)
                return null;
            var match = Regex.Match((string)basetext);
            if (!match.Success)
                return MatchFail == MatchFailDecision.Ignore ? basetext : null;
            return match.Groups[Group].Value;
        }
    }
}
