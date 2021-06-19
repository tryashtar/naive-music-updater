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
        public readonly int? MinLength;
        public IndexValueOperator(int index, OutofBoundsDecision oob, int? min_length = null)
        {
            Index = index;
            OutOfBounds = oob;
            MinLength = min_length;
        }

        public IValue Apply(IMusicItem item, IValue original)
        {
            if (original.IsBlank)
                return BlankValue.Instance;

            var list = original.AsList();
            if (MinLength != null && list.Values.Count < MinLength)
                return BlankValue.Instance;

            int real_index = Index >= 0 ? Index : list.Values.Count + Index;
            if (real_index >= list.Values.Count || real_index < 0)
            {
                if (OutOfBounds == OutofBoundsDecision.Exit || list.Values.Count == 0)
                    return BlankValue.Instance;
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
