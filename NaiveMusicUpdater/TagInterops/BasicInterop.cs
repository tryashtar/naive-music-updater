namespace NaiveMusicUpdater;

public abstract class BacicInterop<T> : AbstractInterop<T> where T : Tag
{
    public BacicInterop(T tag, LibraryConfig config) : base(tag, config)
    {
    }

    public virtual IValue Get(MetadataField field)
    {
    }

    public virtual void Set(MetadataField field, IValue value)
    {
    }
}