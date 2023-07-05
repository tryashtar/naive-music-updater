namespace NaiveMusicUpdater;

public class Id3v2TagInterop : BacicInterop<TagLib.Id3v2.Tag>
{
    private static readonly string[] ReadDelimiters = { "/", "; ", ";" };
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
        // don't set fields that are are going to be cleaned away
        if (field == MetadataField.Title && !Config.ShouldKeepFrame("TIT2"))
            return;
        if (field == MetadataField.Album && !Config.ShouldKeepFrame("TALB"))
            return;
        if (field == MetadataField.AlbumArtists && !Config.ShouldKeepFrame("TPE1"))
            return;
        if (field == MetadataField.Performers && !Config.ShouldKeepFrame("TPE2"))
            return;
        if (field == MetadataField.Arranger && !Config.ShouldKeepFrame("TPE4"))
            return;
        if (field == MetadataField.Composers && !Config.ShouldKeepFrame("TCOM"))
            return;
        if ((field == MetadataField.Track || field == MetadataField.TrackTotal) && !Config.ShouldKeepFrame("TRCK"))
            return;
        if (field == MetadataField.Year && !Config.ShouldKeepFrame("TDRC"))
            return;
        if (field == MetadataField.Genres && !Config.ShouldKeepFrame("TCON"))
            return;
        if (field == MetadataField.Comment && !Config.ShouldKeepFrame("COMM"))
            return;
        if ((field == MetadataField.Disc || field == MetadataField.DiscTotal) && !Config.ShouldKeepFrame("TPOS"))
            return;
        if (field == MetadataField.Art && !Config.ShouldKeepFrame("APIC"))
            return;
        if (field == MetadataField.Language && Config.ShouldKeepFrame("TLAN"))
        {
            var existing = LanguageExtensions.GetId3v2(this.Tag) ?? "(blank)";
            var val = value.IsBlank ? null : value.AsString().Value;
            if (LanguageExtensions.SetId3v2(this.Tag, val))
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {existing} -> {value}");
            return;
        }
        base.Set(field, value);
    }

    public override void Clean()
    {
        var remove = Config.DecideFramesToRemove(Tag).ToList();
        foreach (var frame in remove)
        {
            Logger.WriteLine($"{Tag.TagTypes} frame removed: {TagPrinter.ToString(frame)}");
            Tag.RemoveFrame(frame);
        }
    }
}