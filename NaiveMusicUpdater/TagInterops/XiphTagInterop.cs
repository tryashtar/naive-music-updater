namespace NaiveMusicUpdater;

public class XiphTagInterop : BacicInterop<TagLib.Ogg.XiphComment>
{
    public XiphTagInterop(TagLib.Ogg.XiphComment tag, LibraryConfig config) : base(tag, config)
    {
    }

    protected override ByteVector RenderTag()
    {
        return Tag.Render(false);
    }

    public override IValue Get(MetadataField field)
    {
        if (field == MetadataField.Year)
        {
            var val = Tag.GetField("YEAR");
            return val.Length == 0 ? BlankValue.Instance : new ListValue(val);
        }
        else
            return base.Get(field);
    }

    public override void Set(MetadataField field, IValue value)
    {
        if (field == MetadataField.Year)
        {
            var raw = Get(field);
            var existing = raw.IsBlank ? Array.Empty<string>() : raw.AsList().Values.ToArray();
            var val = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
            if (!existing.SequenceEqual(val))
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                if (value.IsBlank)
                    Tag.RemoveField("YEAR");
                else
                    Tag.SetField("YEAR", val);
            }
        }
        else
            base.Set(field, value);
    }
}