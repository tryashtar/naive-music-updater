namespace NaiveMusicUpdater;

public class ApeTagInterop : AbstractInterop<TagLib.Ape.Tag>
{
    public ApeTagInterop(TagLib.Ape.Tag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }

    protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
    {
        var schema = BasicInterop.BasicSchema(Tag);
        schema.Remove(MetadataField.Arranger);
        return schema;
    }

    protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
    {
        var schema = BasicInterop.BasicWipeSchema(Tag);
        return schema;
    }
}
