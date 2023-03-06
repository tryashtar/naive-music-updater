namespace NaiveMusicUpdater;

public class LiteralValueSource : IValueSource
{
    public readonly IValue Literal;

    public LiteralValueSource(IValue literal)
    {
        Literal = literal;
    }

    public IValue Get(IMusicItem item)
    {
        return Literal;
    }
}