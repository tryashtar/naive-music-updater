namespace NaiveMusicUpdater;

public class Id3v2TagInterop : BacicInterop<TagLib.Id3v2.Tag>
{
    private static readonly string[] ReadDelimiters = new string[] { "/", "; ", ";" };
    private const string WriteDelimiter = "; ";

    public Id3v2TagInterop(TagLib.Id3v2.Tag tag, LibraryConfig config) : base(tag, config)
    {
    }

    protected override void CustomSetup()
    {
        Tag.ReadArtistDelimiters = ReadDelimiters;
        Tag.WriteArtistDelimiter = WriteDelimiter;
    }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }

    public override void Set(MetadataField field, IValue value)
    {
        base.Set(field, value);
    }
}