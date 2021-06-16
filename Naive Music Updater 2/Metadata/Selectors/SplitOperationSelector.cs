using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class SplitOperationSelector : MetadataSelector
    {
        private readonly MetadataSelector From;
        private readonly string Separator;
        private readonly int Index;
        private readonly bool TakeAll;
        private readonly NoSeparatorDecision NoSeparator;
        private readonly OutofBoundsDecision OutofBounds;

        private enum NoSeparatorDecision
        {
            Exit,
            Ignore
        }

        private enum OutofBoundsDecision
        {
            Exit,
            Wrap,
            Clamp
        }

        // gets metadata "From" somewhere else and extracts a part of it by splitting the string and taking one of its pieces
        public SplitOperationSelector(YamlMappingNode yaml)
        {
            From = MetadataSelectorFactory.FromToken(yaml["from"]);
            Separator = (string)yaml["separator"];
            var take_all = yaml.Go("take_all");
            if (take_all != null && bool.Parse((string)take_all))
                TakeAll = true;
            else
                Index = int.Parse((string)yaml["index"]);
            NoSeparator = NoSeparatorDecision.Ignore;
            var no_separator = yaml.Go("no_separator");
            if (no_separator != null && (string)no_separator == "exit")
                NoSeparator = NoSeparatorDecision.Exit;
            OutofBounds = OutofBoundsDecision.Exit;
            var bounds = yaml.Go("out_of_bounds");
            if (bounds != null && (string)bounds == "wrap")
                OutofBounds = OutofBoundsDecision.Wrap;
            if (bounds != null && (string)bounds == "clamp")
                OutofBounds = OutofBoundsDecision.Clamp;
        }

        public override MetadataProperty GetRaw(IMusicItem item)
        {
            var basetext = From.GetRaw(item);
            if (basetext == null)
                return null;
            string[] parts = basetext.Value.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && NoSeparator == NoSeparatorDecision.Exit)
                return null;
            if (TakeAll)
                return MetadataProperty.List(parts.ToList(), CombineMode.Replace);
            int index = Index;
            if (index < 0 || index >= parts.Length)
            {
                if (OutofBounds == OutofBoundsDecision.Exit)
                    return null;
                if (OutofBounds == OutofBoundsDecision.Wrap)
                {
                    index %= parts.Length;
                    index += parts.Length;
                    index %= parts.Length;
                }
                if (OutofBounds == OutofBoundsDecision.Clamp)
                    index = Math.Max(0, Math.Min(parts.Length - 1, index));
            }
            return MetadataProperty.Single(parts[index], CombineMode.Replace);
        }
    }
}
