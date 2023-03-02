namespace NaiveMusicUpdater;

public class ApeTagInterop : BasicInterop<TagLib.Ape.Tag>
{
    public ApeTagInterop(TagLib.Ape.Tag tag, LibraryConfig config) : base(tag, config) { }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }
}
