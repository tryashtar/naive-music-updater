namespace NaiveMusicUpdater;

public class AppleTagInterop : BacicInterop<TagLib.Mpeg4.AppleTag>
{
    public AppleTagInterop(TagLib.Mpeg4.AppleTag tag, LibraryConfig config) : base(tag, config)
    {
    }

    protected override ByteVector RenderTag()
    {
        var vector = new ByteVector();
        foreach (var data in Tag.Select(x => x.Render()))
        {
            vector.Add(data);
        }

        return vector;
    }
}