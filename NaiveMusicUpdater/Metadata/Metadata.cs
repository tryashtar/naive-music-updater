namespace NaiveMusicUpdater;

// an actual mutable collection of metadata
public class Metadata
{
    public readonly Dictionary<MetadataField, IValue> SavedFields = new();

    public void Register(MetadataField field, IValue value)
    {
        SavedFields[field] = value;
    }

    public void Combine(MetadataField field, IValue value, CombineMode mode)
    {
        if (SavedFields.TryGetValue(field, out var existing))
            SavedFields[field] = ValueExtensions.Combine(existing, value, mode);
        else
            SavedFields[field] = value;
    }

    public IValue Get(MetadataField field)
    {
        return SavedFields.TryGetValue(field, out var result) ? result : BlankValue.Instance;
    }

    public void MergeWith(Metadata other, CombineMode mode)
    {
        foreach (var (field, value) in other.SavedFields)
        {
            Combine(field, value, mode);
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