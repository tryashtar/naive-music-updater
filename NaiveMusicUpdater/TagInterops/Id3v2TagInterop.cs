namespace NaiveMusicUpdater;

public class Id3v2TagInterop : AbstractInterop<TagLib.Id3v2.Tag>
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

    protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
    {
        var schema = BasicInterop.BasicSchema(Tag);
        schema[MetadataField.Language] = Delegates(() => Get(LanguageExtensions.Get(Tag)), x => LanguageExtensions.Set(Tag, Value(x)));
        schema[MetadataField.Art] = new InteropDelegates(() =>
            {
                var pic = PictureExtensions.Get(Tag);
                return pic == null
                    ? MetadataProperty.Ignore()
                    : new MetadataProperty(new StringValue(pic.ToString()), CombineMode.Replace);
            },
            x =>
            {
                if (x.Mode == CombineMode.Remove)
                {
                    PictureExtensions.Set(Tag, null);
                    return;
                }
                if (Config.ArtTemplates == null)
                    return;
                foreach (var entry in x.Value.AsList().Values)
                {
                    var pic = Config.ArtTemplates.GetProcessed(entry);
                    if (pic != null)
                    {
                        PictureExtensions.Set(Tag, pic);
                        return;
                    }
                }
            }, StringEqual);
        return schema;
    }

    protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
    {
        var schema = BasicInterop.BasicWipeSchema(Tag);
        schema.Add("compilation", SimpleWipeRet(() => Tag.IsCompilation.ToString(), () => Tag.IsCompilation = false));
        AddFrameWipes(schema);
        return schema;
    }

    private IEnumerable<Frame> UnwantedFrames()
    {
        return Config.DecideFrames(Tag).remove;
    }

    private void AddFrameWipes(Dictionary<string, WipeDelegates> schema)
    {
        schema.Add("unwanted frames", SimpleWipeRet(
            () =>
            {
                var frames = UnwantedFrames();
                return string.Join("\n", frames.Select(FrameViewer.ToString));
            },
            () =>
            {
                var frames = UnwantedFrames().ToList();
                foreach (var frame in frames)
                {
                    Tag.RemoveFrame(frame);
                }

                return frames.Count > 0;
            }));
    }
}