namespace NaiveMusicUpdater;

public class AppleTagInterop : AbstractInterop<TagLib.Mpeg4.AppleTag>
{
    public AppleTagInterop(TagLib.Mpeg4.AppleTag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        var vector = new ByteVector();
        foreach (var data in Tag.Select(x => x.Render()))
        {
            vector.Add(data);
        }
        return vector;
    }

    protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
    {
        var schema = BasicInterop.BasicSchema(Tag);
        return schema;
    }

    protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
    {
        var schema = BasicInterop.BasicWipeSchema(Tag);
        return schema;
    }
}
