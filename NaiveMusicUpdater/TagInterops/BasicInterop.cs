namespace NaiveMusicUpdater;

public abstract class BacicInterop<T> : AbstractInterop<T> where T : Tag
{
    public BacicInterop(T tag, LibraryConfig config) : base(tag, config)
    {
    }

    public override IValue Get(MetadataField field)
    {
        
    }

    public override void Set(MetadataField field, IValue value)
    {
        
    }
}