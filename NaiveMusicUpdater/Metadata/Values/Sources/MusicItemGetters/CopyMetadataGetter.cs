namespace NaiveMusicUpdater;

public class CopyMetadataGetter : IMusicItemValueSource
{
    public readonly MetadataField Desired;
    public CopyMetadataGetter(MetadataField desired)
    {
        Desired = desired;
    }

    public IValue Get(IMusicItem item)
    {
        return item.GetMetadata(Desired.Only).Get(Desired);
    }
}
