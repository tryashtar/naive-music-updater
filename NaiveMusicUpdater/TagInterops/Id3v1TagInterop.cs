namespace NaiveMusicUpdater;

public class Id3v1TagInterop : AbstractInterop<TagLib.Id3v1.Tag>
{
    public Id3v1TagInterop(TagLib.Id3v1.Tag tag, LibraryConfig config) : base(tag, config) { }
    protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
    {
        var schema = BasicInterop.BasicSchema(Tag);
        schema.Remove(MetadataField.AlbumArtists);
        schema.Remove(MetadataField.Composers);
        schema.Remove(MetadataField.Arranger);
        schema.Remove(MetadataField.TrackTotal);
        schema.Remove(MetadataField.Disc);
        schema.Remove(MetadataField.DiscTotal);
        void SetPrimitive(MetadataField field, int length)
        {
            var existing = schema[field];
            schema[field] = new InteropDelegates(existing.Getter, existing.Setter, (a, b) => PrimitiveEqual(a, b, length));
        }
        SetPrimitive(MetadataField.Title, 30);
        SetPrimitive(MetadataField.Performers, 30);
        SetPrimitive(MetadataField.Album, 30);
        SetPrimitive(MetadataField.Comment, 28);
        return schema;
    }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }

    protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
    {
        var schema = BasicInterop.BasicWipeSchema(Tag);
        return schema;
    }

    private static bool PrimitiveEqual(MetadataProperty p1, MetadataProperty p2, int length)
    {
        var value1 = PrimitiveIfy(Array(p1), length);
        var value2 = PrimitiveIfy(Array(p2), length);
        return value1 == value2;
    }

    private static string PrimitiveIfy(string[] values, int length)
    {
        string combined = String.Join(";", values.Select(x => x.Trim()));
        return PrimitiveIfy(combined, length);
    }

    private static string PrimitiveIfy(string value, int length)
    {
        return TagLib.Id3v1.Tag.DefaultStringHandler.Render(value).Resize(length).ToString().Trim().TrimEnd('\0');
    }
}
