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
            Value = value;
        }

        public ListValue AsList()
        {
            return new ListValue(new[] { Value });
        }

        public StringValue AsString()
        {
            return this;
        }

        public bool HasContents => Value != null;
    }
}
