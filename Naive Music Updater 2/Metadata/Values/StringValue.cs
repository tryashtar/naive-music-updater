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

        public MetadataProperty ToProperty()
        {
            return MetadataProperty.Single(Value, CombineMode.Replace);
        }
    }
}
