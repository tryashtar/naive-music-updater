namespace NaiveMusicUpdater;

public class RegexItemPredicate : IItemPredicate
{
    public readonly Regex Matcher;

    public RegexItemPredicate(Regex regex)
    {
        Matcher = regex;
    }

    public bool Matches(IMusicItem item)
    {
        return Matcher.IsMatch(item.SimpleName);
    }

    public override string ToString()
    {
        return Matcher.ToString();
    }
}