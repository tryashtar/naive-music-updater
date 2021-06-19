using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class ModeValueSourceFieldSetter : IFieldSetter
    {
        public readonly CombineMode Mode;
        public readonly IValueSource Source;

        public ModeValueSourceFieldSetter(CombineMode mode, IValueSource source)
        {
            Mode = mode;
            Source = source;
        }

        public MetadataProperty Get(IMusicItem item)
        {
            var value = Source.Get(item);
            if (value.IsBlank)
                return MetadataProperty.Ignore();
            return new MetadataProperty(value, Mode);
        }

        public MetadataProperty GetWithContext(IMusicItem item, IValue value)
        {
            // discard context
            return Get(item);
        }
    }
}
