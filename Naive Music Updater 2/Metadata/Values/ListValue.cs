using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class ListValue : IValue
    {
        public readonly List<string> Values;
        public ListValue(IEnumerable<string> values)
        {
            Values = values.ToList();
        }

        public MetadataProperty ToProperty()
        {
            return MetadataProperty.List(Values, CombineMode.Replace);
        }
    }
}
