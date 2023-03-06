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
        if (field == MetadataField.Comment && !Config.ShouldKeepApe("Comment"))
            return;
        if ((field == MetadataField.Disc || field == MetadataField.DiscTotal) && !Config.ShouldKeepApe("Disc"))
            return;
        if ((field == MetadataField.Track || field == MetadataField.TrackTotal) && !Config.ShouldKeepApe("Track"))
            return;
        if (field == MetadataField.Year && !Config.ShouldKeepApe("Year"))
            return;
        if (field == MetadataField.Album && !Config.ShouldKeepApe("Album"))
            return;
        if (field == MetadataField.Performers && !Config.ShouldKeepApe("Artist"))
            return;
        if (field == MetadataField.AlbumArtists && !Config.ShouldKeepApe("Album Artist"))
            return;
        if (field == MetadataField.Composers && !Config.ShouldKeepApe("Composer"))
            return;
        if (field == MetadataField.Art && !Config.ShouldKeepApe("Cover Art (front)"))
            return;
        base.Set(field, value);
    }

    public override void Clean()
    {
        foreach (var key in Tag.ToList())
        {
            if (!Config.ShouldKeepApe(key))
            {
                Logger.WriteLine($"{Tag.TagTypes} {key} removed: {Tag.GetItem(key)}");
                Tag.RemoveItem(key);
            }
        }
    }
}