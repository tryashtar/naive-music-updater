namespace NaiveMusicUpdater;

public class XiphTagInterop : AbstractInterop<TagLib.Ogg.XiphComment>
{
    public XiphTagInterop(TagLib.Ogg.XiphComment tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render(false);
    }

    protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
    {
        var schema = BasicInterop.BasicSchema(Tag);
        schema[MetadataField.Year] = new InteropDelegates(() => Get(Tag.GetField("YEAR")), x => Tag.SetField("YEAR", Number(x)), NumberEqual);
        return schema;
    }

    protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
    {
        var schema = BasicInterop.BasicWipeSchema(Tag);
        AddFieldWipes(schema);
        return schema;
    }

    private IEnumerable<(string key, string val)> UnwantedMetadata()
    {
        foreach (var key in Tag)
        {
            if (!Config.ShouldKeepXiph(key))
                yield return (key, String.Join("; ", Tag.GetField(key)));
        }
    }

    private void AddFieldWipes(Dictionary<string, WipeDelegates> schema)
    {
        schema.Add("unwanted metadata", SimpleWipeRet(
            () =>
            {
                var unwanted = UnwantedMetadata();
                return string.Join("\n", unwanted.Select(x => $"Key: {x.key}, Value: {x.val}"));
            },
            () =>
            {
                var unwanted = UnwantedMetadata().ToList();
                foreach (var meta in unwanted)
                {
                    Tag.RemoveField(meta.key);
                }
                return unwanted.Count > 0;
            }));
    }
}
