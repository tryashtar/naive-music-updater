namespace NaiveMusicUpdater;

public class FlacTagInterop : MultipleInterop
{
    protected readonly TagLib.Flac.Metadata Tag;
    protected readonly LibraryConfig Config;
    public FlacTagInterop(TagLib.Flac.Metadata tag, LibraryConfig config) : base(tag, config)
    {
        Tag = tag;
        Config = config;
    }

    public override void Set(MetadataField field, IValue value)
    {
        if (field == MetadataField.Art)
        {
            IPicture? pic = null;
            if (!value.IsBlank && Config.ArtTemplates != null)
                pic = Config.ArtTemplates.FirstArt(value.AsList().Values).picture;

            if (pic == null && Tag.Pictures.Length > 0)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Tag.Pictures[0].Description} -> {BlankValue.Instance}");
                Tag.Pictures = Array.Empty<IPicture>();
            }
            else if (pic != null && (Tag.Pictures.Length == 0 || Tag.Pictures[0].Data != pic.Data))
            {
                var prev = Tag.Pictures.Length == 0 ? BlankValue.Instance.ToString() : Tag.Pictures[0].Description;
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {prev} -> {pic.Description}");
                Tag.Pictures = new[] { pic };
            }
        }
        else
            base.Set(field, value);
    }
}
