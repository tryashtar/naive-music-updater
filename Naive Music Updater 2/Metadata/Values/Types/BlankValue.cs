using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class BlankValue : IValue
    {
        public BlankValue()
        {
        }

        public ListValue AsList()
        {
            return new ListValue(Enumerable.Empty<string>());
        }

        public StringValue AsString()
        {
            return new StringValue(null);
        }

        public bool HasContents => false;
    }
}
