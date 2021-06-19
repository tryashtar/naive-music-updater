using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class StringValue : IValue
    {
        public readonly string Value;
        public StringValue(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Value = value;
        }

        public ListValue AsList() => new(Value);
        public StringValue AsString() => this;
        public bool IsBlank => false;

        public override string ToString() => Value;
    }
}
