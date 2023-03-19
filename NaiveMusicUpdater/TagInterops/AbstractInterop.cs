namespace NaiveMusicUpdater;

public abstract class AbstractInterop<T> : ITagInterop where T : Tag
{
    protected readonly T Tag;
    protected readonly LibraryConfig Config;
    private readonly ByteVector OriginalVector;

    // easy way to check if the tag has changed
    // however, every tag type has a different way of doing this
    public bool Changed => OriginalVector != RenderTag();

    public AbstractInterop(T tag, LibraryConfig config)
    {
        Tag = tag;
        Config = config;
        CustomSetup();
        OriginalVector = RenderTag();
    }

    // maybe this can go in the child constructor
    // not sure if the implementations cause RenderTag to return something different
    protected virtual void CustomSetup()
    {
    }

    protected abstract ByteVector RenderTag();

    public virtual void Clean()
    {
    }

    public abstract IValue Get(MetadataField field);
    public abstract void Set(MetadataField field, IValue value);
}