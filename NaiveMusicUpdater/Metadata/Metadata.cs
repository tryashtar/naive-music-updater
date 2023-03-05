namespace NaiveMusicUpdater;

// an actual mutable collection of metadata
public class Metadata
{
    public readonly Dictionary<MetadataField, IValue> SavedFields = new();

    public void Register(MetadataField field, IValue value)
    {
        SavedFields[field] = value;
    }

    public IValue Get(MetadataField field)
    {
        return SavedFields.TryGetValue(field, out var result) ? result : BlankValue.Instance;
    }

    public void MergeWith(Metadata other, CombineMode mode)
    {
        foreach (var pair in other.SavedFields)
        {
            if (SavedFields.TryGetValue(pair.Key, out var existing))
                SavedFields[pair.Key] = ValueExtensions.Combine(existing, pair.Value, mode);
            else
                SavedFields[pair.Key] = pair.Value;
        }
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        foreach (var item in SavedFields)
        {
            builder.AppendLine($"{item.Key.DisplayName}: {item.Value}");
        }

        return builder.ToString();
    }
}