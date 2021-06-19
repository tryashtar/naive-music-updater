using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class DirectValueSourceFieldSetter : IFieldSetter
    {
        public readonly IValueSource Source;

        public DirectValueSourceFieldSetter(IValueSource source)
        {
            Source = source;
        }

        public MetadataProperty Get(IMusicItem item)
        {
            var value = Source.Get(item);
            if (value.IsBlank)
                return MetadataProperty.Ignore();
            return new MetadataProperty(value, CombineMode.Replace);
        }

        public MetadataProperty GetWithContext(IMusicItem item, IValue value)
        {
            // discard context
            return Get(item);
        }
    }
}
