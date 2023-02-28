namespace NaiveMusicUpdater;

public class RegexValueOperator : IValueOperator
{
    public readonly Regex RegexItem;
    public readonly MatchFailDecision MatchFail;

    public RegexValueOperator(Regex regex, MatchFailDecision decision)
    {
        RegexItem = regex;
        MatchFail = decision;
    }

    public IValue Apply(IMusicItem item, IValue original)
    {
        if (original.IsBlank)
            return BlankValue.Instance;

        var text = original.AsString();

        var match = RegexItem.Match(text.Value);
        if (!match.Success)
            return MatchFail == MatchFailDecision.TakeWhole ? original : BlankValue.Instance;

        return new RegexMatchValue(match);
    }
}

public enum MatchFailDecision
{
    Exit,
    TakeWhole
}
