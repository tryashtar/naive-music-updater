namespace NaiveMusicUpdater;

public class RiffTagInterop : AbstractInterop<TagLib.Riff.InfoTag>
{
    public RiffTagInterop(TagLib.Riff.InfoTag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
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

public class MovieTagInterop : AbstractInterop<TagLib.Riff.MovieIdTag>
{
    public MovieTagInterop(TagLib.Riff.MovieIdTag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }

    protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
    {
        var schema = BasicInterop.BasicSchema(Tag);
        schema.Remove(MetadataField.Album);
        schema.Remove(MetadataField.AlbumArtists);
        schema.Remove(MetadataField.Composers);
        schema.Remove(MetadataField.Year);
        return schema;
    }

    protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
    {
        var schema = BasicInterop.BasicWipeSchema(Tag);
        return schema;
    }
}

public class DivTagInterop : AbstractInterop<TagLib.Riff.DivXTag>
{
    public DivTagInterop(TagLib.Riff.DivXTag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }

    protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
    {
        var schema = BasicInterop.BasicSchema(Tag);
        schema.Remove(MetadataField.Album);
        schema.Remove(MetadataField.AlbumArtists);
        schema.Remove(MetadataField.Composers);
        return schema;
    }

    protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
    {
        var schema = BasicInterop.BasicWipeSchema(Tag);
        return schema;
    }
}
