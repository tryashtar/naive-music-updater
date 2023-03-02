namespace NaiveMusicUpdater;

public class RiffTagInterop : BacicInterop<TagLib.Riff.InfoTag>
{
    public RiffTagInterop(TagLib.Riff.InfoTag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }
}

public class MovieTagInterop : BacicInterop<TagLib.Riff.MovieIdTag>
{
    public MovieTagInterop(TagLib.Riff.MovieIdTag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }
}

public class DivTagInterop : BacicInterop<TagLib.Riff.DivXTag>
{
    public DivTagInterop(TagLib.Riff.DivXTag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }
}
