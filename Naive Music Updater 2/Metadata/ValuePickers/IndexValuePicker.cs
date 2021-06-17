using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class IndexValuePicker : IValuePicker
    {
        public readonly int Index;
        public readonly OutofBoundsDecision OutOfBounds;
        public IndexValuePicker(int index, OutofBoundsDecision oob)
        {
            Index = index;
            OutOfBounds = oob;
        }

        public MetadataProperty PickFrom(MetadataProperty full)
        {
            int real_index = Index >= 0 ? Index : full.ListValue.Count + Index;
            if (real_index >= full.ListValue.Count)
            {
                if (OutOfBounds == OutofBoundsDecision.Exit || full.ListValue.Count == 0)
                    return MetadataProperty.Ignore();
                else if (OutOfBounds == OutofBoundsDecision.Clamp)
                    real_index = Math.Clamp(real_index, 0, full.ListValue.Count - 1);
                else if (OutOfBounds == OutofBoundsDecision.Wrap)
                    real_index %= full.ListValue.Count;
            }
            return MetadataProperty.Single(full.ListValue[real_index], full.CombineMode);
        }
    }

    public enum OutofBoundsDecision
    {
        Exit,
        Wrap,
        Clamp
    }
}
