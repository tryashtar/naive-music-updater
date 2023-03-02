namespace NaiveMusicUpdater;

public interface IMusicItemValueSource
{
    IValue Get(IMusicItem item);
}

public static class MusicItemGetterFactory
{
    public static Dictionary<NameType, IMusicItemValueSource> NameGetters = new()
    {
        [NameType.FileName] = new FuncGetter(x => new StringValue(x.SimpleName)),
        [NameType.CleanName] = new FuncGetter(x => new StringValue(x.GlobalConfig.CleanName(x.SimpleName))),
        [NameType.Path] = new FuncGetter(x => new StringValue(Util.StringPathAfterRoot(x)))
    };

    public static IMusicItemValueSource Create(YamlNode yaml)
    {
        if (yaml is YamlScalarNode scalar)
        {
            var name = scalar.ToEnum<NameType>();
            return NameGetters[name.Value];
        }
        else if (yaml is YamlMappingNode map)
        {
            var copy = yaml.Go("copy").NullableParse(x => MetadataField.FromID(x.String()));
            if (copy != null)
                return new CopyMetadataGetter(copy);
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