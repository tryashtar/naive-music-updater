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
        public readonly bool IsList;
        public string Value { get; private set; }
        public readonly List<string> ListValue;
        public readonly CombineMode CombineMode;

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
            return new MetadataProperty(false, null, new List<string>(), CombineMode.Replace);
        }

        public static MetadataProperty Ignore()
        {
            return new MetadataProperty(false, null, new List<string>(), CombineMode.Ignore);
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
            if (other.CombineMode == CombineMode.Replace)
            {
                ListValue.Clear();
                ListValue.AddRange(other.ListValue);
                Value = other.Value;
            }
            if (other.CombineMode == CombineMode.Append)
            {
                ListValue.AddRange(other.ListValue);
                Value = ListValue.FirstOrDefault();
            }
            if (other.CombineMode == CombineMode.Prepend)
            {
                ListValue.InsertRange(0, other.ListValue);
                Value = ListValue.FirstOrDefault();
            }
        }
    }

    public enum CombineMode
    {
        Ignore,
        Replace,
        Append,
        Prepend
    }
}
