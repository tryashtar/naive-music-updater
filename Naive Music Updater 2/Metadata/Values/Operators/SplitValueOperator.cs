using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class SplitValueOperator : IValueOperator
    {
        public readonly string Separator;
        public readonly NoSeparatorDecision NoSeparator;

        public SplitValueOperator(string separator, NoSeparatorDecision decision)
        {
            Separator = separator;
            NoSeparator = decision;
        }

        public IValue Apply(IMusicItem item, IValue original)
        {
            if (original.IsBlank)
                return BlankValue.Instance;

            var text = original.AsString();

            string[] parts = text.Value.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && NoSeparator == NoSeparatorDecision.Exit)
                return BlankValue.Instance;

            return new ListValue(parts);
        }
    }

    public enum NoSeparatorDecision
    {
        Exit,
        Ignore
    }
}
