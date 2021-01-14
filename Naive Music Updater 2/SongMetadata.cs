using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
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

    public class Metadata
    {
        private readonly Dictionary<MetadataField, MetadataProperty> SavedFields = new Dictionary<MetadataField, MetadataProperty>();
        public Metadata()
        { }

        public void Register(MetadataField field, MetadataProperty value)
        {
            SavedFields[field] = value;
        }

        public MetadataProperty Get(MetadataField field)
        {
            if (SavedFields.TryGetValue(field, out var result))
                return result;
            return MetadataProperty.Ignore();
        }

        public void Merge(Metadata other)
        {
            foreach (var pair in other.SavedFields)
            {
                if (SavedFields.TryGetValue(pair.Key, out var existing))
                    existing.CombineWith(pair.Value);
                else
                    SavedFields[pair.Key] = pair.Value;
            }
        }

        public static Metadata FromMany(IEnumerable<Metadata> many)
        {
            var result = new Metadata();
            foreach (var item in many)
            {
                result.Merge(item);
            }
            return result;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var item in SavedFields)
            {
                builder.AppendLine($"{item.Key.Name}: {String.Join(";", item.Value.ListValue)}");
            }
            return builder.ToString();
        }
    }
}
