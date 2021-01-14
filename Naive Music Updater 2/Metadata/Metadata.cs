using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    // an actual mutable collection of metadata
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
