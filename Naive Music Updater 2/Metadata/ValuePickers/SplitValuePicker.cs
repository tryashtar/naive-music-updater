using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class SplitValuePicker : IValuePicker
    {
        public readonly string Separator;
        public readonly NoSeparatorDecision NoSeparator;

        public SplitValuePicker(YamlMappingNode yaml)
        {
            Separator = (string)yaml["separator"];
            NoSeparator = yaml.ParseOrDefault("no_separator", x => Util.ParseUnderscoredEnum<NoSeparatorDecision>((string)x), NoSeparatorDecision.Ignore);
        }

        public MetadataProperty PickFrom(MetadataProperty full)
        {
            var basetext = full.Value;
            string[] parts = basetext.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && NoSeparator == NoSeparatorDecision.Exit)
                return MetadataProperty.Ignore();
            return MetadataProperty.List(parts.ToList(), full.CombineMode);
        }
    }

    public enum NoSeparatorDecision
    {
        Exit,
        Ignore
    }
}
