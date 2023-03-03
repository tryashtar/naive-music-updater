namespace NaiveMusicUpdater;

public class ApeTagInterop : BacicInterop<TagLib.Ape.Tag>
{
    public ApeTagInterop(TagLib.Ape.Tag tag, LibraryConfig config) : base(tag, config)
    {
    }

    protected override ByteVector RenderTag()
    {
        return Tag.Render();
    }

    public override void Set(MetadataField field, IValue value)
    {
        if (field == MetadataField.Arranger)
            return;
        if (field == MetadataField.Title && !Config.ShouldKeepApe("Title"))
            return;
        if (field == MetadataField.Album && !Config.ShouldKeepApe("Album"))
            return;
        if (field == MetadataField.Performers && !Config.ShouldKeepApe("Artist"))
            return;
        if (field == MetadataField.AlbumArtists && !Config.ShouldKeepApe("Album Artist"))
            return;
        if (field == MetadataField.Composers && !Config.ShouldKeepApe("Composer"))
            return;
        base.Set(field, value);
    }
}