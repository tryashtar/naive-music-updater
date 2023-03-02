namespace NaiveMusicUpdater;

public class Id3v1TagInterop : BacicInterop<TagLib.Id3v1.Tag>
{
    public Id3v1TagInterop(TagLib.Id3v1.Tag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }
}
