namespace NaiveMusicUpdater;

// we only want to save files that were actually changed
// while TagLib does a good job unifying different tag types under a shared class,
// we need to be more specific about which tags we're saving to in order to determine when this is the case
public interface ITagInterop
{
    // used by reverse configs to get pre-existing values
    IValue Get(MetadataField field);

    void Set(MetadataField field, IValue value);

    // remove tags/frames that are configured as undesired
    // (which may not have corresponding fields)
    void Clean();
    bool Changed { get; }
}

public static class TagInteropExtensions
{
    public static Metadata GetFullMetadata(this ITagInterop interop, Predicate<MetadataField> desired)
    {
        var meta = new Metadata();
        foreach (var field in MetadataField.Values)
        {
            if (desired(field))
                meta.Register(field, interop.Get(field));
        }

        return meta;
    }
}

public static class TagInteropFactory
{
    // use dynamic to find the best match at runtime
    public static ITagInterop GetDynamicInterop(dynamic tag, LibraryConfig config)
    {
        return GetInterop(tag, config);
    }

    private static ITagInterop GetInterop(TagLib.Id3v2.Tag tag, LibraryConfig config) =>
        new Id3v2TagInterop(tag, config);

    private static ITagInterop GetInterop(TagLib.Id3v1.Tag tag, LibraryConfig config) =>
        new Id3v1TagInterop(tag, config);

    private static ITagInterop GetInterop(TagLib.Ape.Tag tag, LibraryConfig config) => new ApeTagInterop(tag, config);

    private static ITagInterop GetInterop(TagLib.Ogg.XiphComment tag, LibraryConfig config) =>
        new XiphTagInterop(tag, config);

    private static ITagInterop GetInterop(TagLib.Mpeg4.AppleTag tag, LibraryConfig config) =>
        new AppleTagInterop(tag, config);

    private static ITagInterop GetInterop(TagLib.Riff.InfoTag tag, LibraryConfig config) =>
        new RiffTagInterop(tag, config);

    private static ITagInterop GetInterop(TagLib.Riff.MovieIdTag tag, LibraryConfig config) =>
        new MovieTagInterop(tag, config);

    private static ITagInterop GetInterop(TagLib.Riff.DivXTag tag, LibraryConfig config) =>
        new DivTagInterop(tag, config);

    private static ITagInterop GetInterop(CombinedTag tag, LibraryConfig config) => new MultipleInterop(tag, config);

    private static ITagInterop GetInterop(TagLib.Ogg.GroupedComment tag, LibraryConfig config) =>
        new MultipleXiphInterop(tag, config);

    private static ITagInterop GetInterop(TagLib.Flac.Metadata tag, LibraryConfig config) =>
        new FlacTagInterop(tag, config);
}