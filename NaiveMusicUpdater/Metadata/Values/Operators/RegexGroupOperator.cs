namespace NaiveMusicUpdater;

public class RegexGroupOperator : IValueOperator
{
    public readonly string Group;

    public RegexGroupOperator(string group)
    {
        Group = group;
    }

    public IValue? Apply(IMusicItem item, IValue original)
    {
        return original is not RegexMatchValue text ? null : new StringValue(text.GetGroup(Group));
    }
}