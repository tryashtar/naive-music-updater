using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class NumberValue : IValue
    {
        public readonly uint Value;
        public NumberValue(uint value)
        {
            Value = value;
        }

        public ListValue AsList() => new(Value.ToString());
        public StringValue AsString() => new(Value.ToString());
        public bool IsBlank => false;

        public override string ToString() => Value.ToString();
    }
}
