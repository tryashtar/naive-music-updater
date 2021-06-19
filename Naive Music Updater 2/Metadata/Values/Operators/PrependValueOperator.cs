using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class PrependValueOperator : IValueOperator
    {
        public readonly IValueSource Prepend;
        public PrependValueOperator(IValueSource prepend)
        {
            Prepend = prepend;
        }

        public IValue Apply(IMusicItem item, IValue original)
        {
            var text = original.AsString();
            var extra = Prepend.Get(item).AsString();

            return new StringValue(extra.Value + text.Value);
        }
    }
}
