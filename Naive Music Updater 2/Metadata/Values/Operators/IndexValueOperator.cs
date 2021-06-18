using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class IndexValueOperator : IValueOperator
    {
        public readonly int Index;
        public readonly OutofBoundsDecision OutOfBounds;
        public IndexValueOperator(int index, OutofBoundsDecision oob)
        {
            Index = index;
            OutOfBounds = oob;
        }

        public IValue Apply(IValue original)
        {
            var list = (ListValue)original;

            int real_index = Index >= 0 ? Index : list.Values.Count + Index;
            if (real_index >= list.Values.Count)
            {
                if (OutOfBounds == OutofBoundsDecision.Exit || list.Values.Count == 0)
                    return MetadataProperty.Ignore();
                else if (OutOfBounds == OutofBoundsDecision.Clamp)
                    real_index = Math.Clamp(real_index, 0, list.Values.Count - 1);
                else if (OutOfBounds == OutofBoundsDecision.Wrap)
                    real_index %= list.Values.Count;
            }

            return new StringValue(list.Values[real_index]);
        }
    }

    public enum OutofBoundsDecision
    {
        Exit,
        Wrap,
        Clamp
    }
}
