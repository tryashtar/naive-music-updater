namespace NaiveMusicUpdater;

public class LiteralListSource : IValueSource
{
    private readonly List<string> Literal;
    public LiteralListSource(IEnumerable<string> literal)
    {
        Literal = literal.ToList();
    }

    public IValue Get(IMusicItem item)
    {
        return new ListValue(Literal);
    }
}
