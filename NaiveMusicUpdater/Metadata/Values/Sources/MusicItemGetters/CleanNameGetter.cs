namespace NaiveMusicUpdater;

public class CleanNameGetter : IMusicItemValueSource
{
    public static readonly CleanNameGetter Instance = new();

    public IValue Get(IMusicItem item)
    {
        return new StringValue(item.GlobalCache.Config.CleanName(item.SimpleName));
    }
}
