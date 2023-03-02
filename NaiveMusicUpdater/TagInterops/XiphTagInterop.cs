namespace NaiveMusicUpdater;

public class XiphTagInterop : BacicInterop<TagLib.Ogg.XiphComment>
{
    public XiphTagInterop(TagLib.Ogg.XiphComment tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render(false);
    }
}
