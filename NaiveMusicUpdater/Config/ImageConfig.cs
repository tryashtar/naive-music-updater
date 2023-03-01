namespace NaiveMusicUpdater;

public class ImageConfig
{
    private readonly ImageSettings AllImages;
    private readonly List<TargetedStrategy> MetadataStrategies;
    private readonly List<TargetedStrategy> SharedStrategies;

    public ImageConfig(string file)
    {
        var yaml = YamlHelper.ParseFile(file);
    }
}

public class ImageSettings
{
    public readonly int Width;
    public readonly int Height;
    public readonly IScaling Scaling;
}

public interface IScaling
{
    // ignore WH, use original size
    // stretch to fill WH
    // scale to smaller of WH while maintaining aspect ratio
    // 
}