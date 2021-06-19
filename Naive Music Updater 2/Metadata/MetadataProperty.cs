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
    public class MetadataProperty : IValue
    {
        public readonly bool IsList;
        public string Value { get; private set; }
        public readonly List<string> ListValue;
        public CombineMode CombineMode { get; private set; }

        public static MetadataProperty Single(string value, CombineMode mode)
        {
            return new MetadataProperty(false, value, new List<string> { value }, mode);
        }

        public static MetadataProperty List(List<string> value, CombineMode mode)
        {
            return new MetadataProperty(true, value.FirstOrDefault(), value, mode);
        }

        public static MetadataProperty Delete()
        {
            return new MetadataProperty(false, null, new List<string>(), CombineMode.Remove);
        }

        public static MetadataProperty Ignore()
        {
            return new MetadataProperty(false, null, new List<string>(), CombineMode.Ignore);
        }

        public static MetadataProperty FromValue(IValue value, CombineMode mode)
        {
            if (value is StringValue str)
                return MetadataProperty.Single(str.Value, mode);
            if (value is ListValue list)
                return MetadataProperty.List(list.Values, mode);
            if (value is MetadataProperty meta)
                return new MetadataProperty(meta.IsList, meta.Value, meta.ListValue, mode);
            if (value is BlankValue blank)
                return MetadataProperty.Ignore();
            throw new InvalidOperationException($"Can't turn {value} into a metadata property");
        }

        private MetadataProperty(bool is_list, string item, List<string> list, CombineMode mode)
        {
            IsList = is_list;
            Value = item;
            ListValue = list;
            CombineMode = mode;
        }

        public void CombineWith(MetadataProperty other)
        {
            switch (other.CombineMode)
            {
                case CombineMode.Ignore:
                    break;
                case CombineMode.Replace:
                    ListValue.Clear();
                    ListValue.AddRange(other.ListValue);
                    Value = other.Value;
                    CombineMode = CombineMode.Replace;
                    break;
                case CombineMode.Append:
                    ListValue.AddRange(other.ListValue);
                    Value = ListValue.FirstOrDefault();
                    CombineMode = CombineMode.Replace;
                    break;
                case CombineMode.Prepend:
                    ListValue.InsertRange(0, other.ListValue);
                    Value = ListValue.FirstOrDefault();
                    CombineMode = CombineMode.Replace;
                    break;
                case CombineMode.Remove:
                    ListValue.Clear();
                    Value = null;
                    CombineMode = CombineMode.Replace;
                    break;
                default:
                    break;
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
            return String.Join("; ", ListValue);
        }

        public StringValue AsString()
        {
            return new StringValue(Value);
        }

        public ListValue AsList()
        {
            return new ListValue(ListValue);
        }

        public bool HasContents => CombineMode != CombineMode.Ignore;
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
