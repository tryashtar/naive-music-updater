namespace NaiveMusicUpdater;

public class MultipleInterop : ITagInterop
{
    private readonly List<ITagInterop> Interops;
    public virtual bool Changed => Interops.Any(x => x.Changed);
    public MultipleInterop(CombinedTag tag, LibraryConfig config)
    {
        Interops = tag.Tags.Select(x => TagInteropFactory.GetDynamicInterop(x, config)).ToList();
    }

    public virtual IValue Get(MetadataField field)
    {
        foreach (var interop in Interops)
        {
            var result = interop.Get(field);
            if (!result.IsBlank)
                return result;
        }
        return BlankValue.Instance;
    }

    public virtual void Set(MetadataField field, IValue value)
    {
        foreach (var interop in Interops)
        {
            interop.Set(field, value);
        }
    }

    public virtual void Clean()
    {
        foreach (var interop in Interops)
        {
            interop.Clean();
        }
    }
}

public class MultipleXiphInterop : ITagInterop
{
    private readonly List<ITagInterop> Interops;
    public bool Changed => Interops.Any(x => x.Changed);
    public MultipleXiphInterop(TagLib.Ogg.GroupedComment tag, LibraryConfig config)
    {
        Interops = tag.Comments.Select(x => TagInteropFactory.GetDynamicInterop(x, config)).ToList();
    }

    public IValue Get(MetadataField field)
    {
        foreach (var interop in Interops)
        {
            var result = interop.Get(field);
            if (!result.IsBlank)
                return result;
        }
        return BlankValue.Instance;
    }

    public void Set(MetadataField field, IValue value)
    {
        foreach (var interop in Interops)
        {
            interop.Set(field, value);
        }
    }
    
    public void Clean()
    {
        foreach (var interop in Interops)
        {
            interop.Clean();
        }
    }
}
