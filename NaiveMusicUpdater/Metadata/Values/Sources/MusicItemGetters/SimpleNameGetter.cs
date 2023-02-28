namespace NaiveMusicUpdater;

public class SimpleNameGetter : IMusicItemValueSource
{
    public static readonly SimpleNameGetter Instance = new();

    public IValue Get(IMusicItem item)
    {
        return new StringValue(item.SimpleName);
    }
}
