namespace NaiveMusicUpdater;

public abstract class AbstractInterop<T> : ITagInterop where T : Tag
{
    protected readonly T Tag;
    protected readonly LibraryConfig Config;
    private readonly TagTypes TagType;
    private readonly ByteVector OriginalVector;

    public bool Changed => OriginalVector != RenderTag();

    public AbstractInterop(T tag, LibraryConfig config)
    {
        Tag = tag;
        Config = config;
        TagType = tag.TagTypes;
        CustomSetup();
        OriginalVector = RenderTag();
    }

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