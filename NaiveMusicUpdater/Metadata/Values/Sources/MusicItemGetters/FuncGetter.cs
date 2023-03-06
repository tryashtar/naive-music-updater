namespace NaiveMusicUpdater;

public class FuncGetter : IMusicItemValueSource
{
    public delegate IValue Getter(IMusicItem item);

    private readonly Getter Function;

    public FuncGetter(Getter func)
    {
        Function = func;
    }

    public IValue Get(IMusicItem item)
    {
        return Function(item);
    }
}