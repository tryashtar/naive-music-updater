using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    // an actual value of some kind of metadata
    // can be both a list, like artists, or not, like year
    // when a list, you can still access the first value with Value
    // and when not a list, you can still access the singular value with ListValue
    // CombineMode determines whether this replaces other properties when merging
    public class MetadataProperty
    {
        public IValue Value { get; private set; }
        public CombineMode Mode { get; private set; }

        public MetadataProperty(IValue value, CombineMode mode)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Mode = mode;
        }

        public static MetadataProperty Ignore() => new(BlankValue.Instance, CombineMode.Ignore);
        public static MetadataProperty Delete() => new(BlankValue.Instance, CombineMode.Remove);

        public void CombineWith(MetadataProperty other)
        {
            switch (other.Mode)
            {
                case CombineMode.Ignore:
                    break;
                case CombineMode.Replace:
                    Value = other.Value;
                    Mode = CombineMode.Replace;
                    break;
                case CombineMode.Append:
                    Value = new ListValue(this.Value.AsList().Values.Concat(other.Value.AsList().Values));
                    Mode = CombineMode.Replace;
                    break;
                case CombineMode.Prepend:
                    Value = new ListValue(other.Value.AsList().Values.Concat(this.Value.AsList().Values));
                    Mode = CombineMode.Replace;
                    break;
                case CombineMode.Remove:
                    Value = BlankValue.Instance;
                    Mode = CombineMode.Replace;
                    break;
                default:
                    throw new ArgumentException($"Invalid combine mode {other.Mode}");
            }
        }

        public static MetadataProperty Combine(MetadataProperty p1, MetadataProperty p2)
        {
            var property = MetadataProperty.Ignore();
            property.CombineWith(p1);
            property.CombineWith(p2);
            return property;
        }

        public override string ToString()
        {
            if (Value.IsBlank)
                return "(blank)";
            return Value.ToString();
        }
    }

    public enum CombineMode
    {
        Ignore,
        Replace,
        Append,
        Prepend,
        Remove
    }
}
