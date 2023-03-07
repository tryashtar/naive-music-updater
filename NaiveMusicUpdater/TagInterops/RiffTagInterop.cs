namespace NaiveMusicUpdater;

public class RiffTagInterop : BacicInterop<TagLib.Riff.InfoTag>
{
    public RiffTagInterop(TagLib.Riff.InfoTag tag, LibraryConfig config) : base(tag, config)
    {
    }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }
}

public class MovieTagInterop : BacicInterop<TagLib.Riff.MovieIdTag>
{
    public MovieTagInterop(TagLib.Riff.MovieIdTag tag, LibraryConfig config) : base(tag, config)
    {
    }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }

    private static readonly HashSet<MetadataField> Supported = new()
    {
        MetadataField.Title,
        MetadataField.Performers,
        MetadataField.Comment,
        MetadataField.Genres,
        MetadataField.Track,
        MetadataField.TrackTotal
    };

    public override void Set(MetadataField field, IValue value)
    {
        if (!Supported.Contains(field))
            return;
        base.Set(field, value);
    }
}

public class DivTagInterop : BacicInterop<TagLib.Riff.DivXTag>
{
    public DivTagInterop(TagLib.Riff.DivXTag tag, LibraryConfig config) : base(tag, config)
    {
    }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }


    private static readonly HashSet<MetadataField> Supported = new()
    {
        MetadataField.Title,
        MetadataField.Performers,
        MetadataField.Comment,
        MetadataField.Genres,
        MetadataField.Year
    };

    public override void Set(MetadataField field, IValue value)
    {
        if (!Supported.Contains(field))
            return;
        base.Set(field, value);
    }
}