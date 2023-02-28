namespace NaiveMusicUpdater;

public class LiteralStringSource : IValueSource
{
    public readonly string Literal;
    public LiteralStringSource(string literal)
    {
        Literal = literal;
    }

    public IValue Get(IMusicItem item)
    {
        return new StringValue(Literal);
    }
}
