namespace NaiveMusicUpdater;

public class RegexGroupOperator : IValueOperator
{
    public readonly string Group;

    public RegexGroupOperator(string group)
    {
        Group = group;
    }

    public IValue Apply(IMusicItem item, IValue original)
    {
        var text = (RegexMatchValue)original;
        return new StringValue(text.GetGroup(Group));
    }
}
