using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class MultipleValueOperator : IValueOperator
    {
        private readonly List<IValueOperator> Operators;

        public MultipleValueOperator(IEnumerable<IValueOperator> operators)
        {
            Operators = operators.ToList();
        }

        public IValue Apply(IMusicItem item, IValue original)
        {
            foreach (var op in Operators)
            {
                original = op.Apply(item, original);
            }
            return original;
        }
    }
}
