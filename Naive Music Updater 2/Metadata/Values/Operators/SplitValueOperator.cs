using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class SplitValueOperator : IValueOperator
    {
        public readonly string Separator;
        public readonly NoSeparatorDecision NoSeparator;

        public SplitValueOperator(YamlMappingNode yaml)
        {
            Separator = (string)yaml["separator"];
            NoSeparator = yaml.ParseOrDefault("no_separator", x => Util.ParseUnderscoredEnum<NoSeparatorDecision>((string)x), NoSeparatorDecision.Ignore);
        }

        public IValue Apply(IValue original)
        {
            var text = (StringValue)original;

            string[] parts = text.Value.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && NoSeparator == NoSeparatorDecision.Exit)
                return MetadataProperty.Ignore();

            return new ListValue(parts);
        }
    }

    public enum NoSeparatorDecision
    {
        Exit,
        Ignore
    }
}
