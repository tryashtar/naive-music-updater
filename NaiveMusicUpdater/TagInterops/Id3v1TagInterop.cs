namespace NaiveMusicUpdater;

public class Id3v1TagInterop : BacicInterop<TagLib.Id3v1.Tag>
{
    public Id3v1TagInterop(TagLib.Id3v1.Tag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }

    public override void Set(MetadataField field, IValue value)
    {
        if (field == MetadataField.Title)
        {
            var val = value.IsBlank ? null : Trim(value.AsString().Value, 30);
            if (Tag.Title != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Title = val;
            }
        }
        else if (field == MetadataField.Album)
        {
            var val = value.IsBlank ? null : Trim(value.AsString().Value, 30);
            if (Tag.Album != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Album = val;
            }
        }
        else if (field == MetadataField.Comment)
        {
            var val = value.IsBlank ? null : Trim(value.AsString().Value, 28);
            if (Tag.Comment != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Comment = val;
            }
        }
        else if (field == MetadataField.Performers)
        {
            var val = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
            if (!Tag.Performers.SequenceEqual(val))
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Performers = val;
            }
        }
    }

    private static string Trim(string value, int length)
    {
        return TagLib.Id3v1.Tag.DefaultStringHandler.Render(value).Resize(length).ToString().Trim().TrimEnd('\0');
    }
}
