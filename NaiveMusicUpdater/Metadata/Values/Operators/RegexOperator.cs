namespace NaiveMusicUpdater;

public class RegexOperator : IValueOperator
{
    public readonly Regex RegexItem;
    public readonly MatchFailDecision MatchFail;

    public RegexOperator(Regex regex, MatchFailDecision decision)
    {
        RegexItem = regex;
        MatchFail = decision;
    }

    public IValue? Apply(IMusicItem item, IValue original)
    {
        var text = original.AsString();

        var match = RegexItem.Match(text.Value);
        if (!match.Success)
            return MatchFail == MatchFailDecision.TakeWhole ? original : null;

        return new RegexMatchValue(match);
    }
}

public enum MatchFailDecision
{
    Exit,
    TakeWhole
}