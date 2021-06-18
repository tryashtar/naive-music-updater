using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class ModeContextFieldSetter : IFieldSetter
    {
        public readonly CombineMode Mode;
        public readonly IValueOperator Modify;

        public ModeContextFieldSetter(CombineMode mode, IValueOperator modify)
        {
            Mode = mode;
            Modify = modify;
        }

        public MetadataProperty Get(IMusicItem item)
        {
            throw new InvalidOperationException($"Performing an operation on a value requires context!");
        }

        public MetadataProperty GetWithContext(IMusicItem item, IValue value)
        {
            value = Modify.Apply(value);
            return MetadataProperty.FromValue(value, Mode);
        }
    }
}
