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

        public IValue Apply(IValue original)
        {
            foreach (var item in Operators)
            {
                original = item.Apply(original);
            }
            return original;
        }
    }
}
