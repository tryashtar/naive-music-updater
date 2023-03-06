namespace NaiveMusicUpdater;

public interface IMusicItemValueSource
{
    IValue Get(IMusicItem item);
}

public static class MusicItemGetterFactory
{
    public static readonly Dictionary<NameType, IMusicItemValueSource> NameGetters = new()
    {
        [NameType.FileName] = new FuncGetter(x => new StringValue(x.SimpleName)),
        [NameType.CleanName] =
            new FuncGetter(x => new StringValue(x.RootLibrary.LibraryConfig.CleanName(x.SimpleName))),
        [NameType.Path] = new FuncGetter(x => new StringValue(x.StringPathAfterRoot()))
    };

    public static IMusicItemValueSource Create(YamlNode yaml)
    {
        switch (yaml)
        {
            case YamlScalarNode scalar:
            {
                var name = scalar.ToEnum<NameType>();
                return NameGetters[name.Value];
            }
            case YamlMappingNode map:
            {
                var copy = map.Go("copy").NullableParse(x => MetadataField.FromID(x.String()));
                if (copy != null)
                    return new CopyMetadataGetter(copy);
                break;
            }
        }

        throw new ArgumentException($"Can't make metadata selector from {yaml}");
    }
}

public enum NameType
{
    CleanName,
    FileName,
    Path
}