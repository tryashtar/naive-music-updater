namespace NaiveMusicUpdater;

public class RegexMatchValue : IValue
{
    public readonly Match Match;

    public RegexMatchValue(Match match)
    {
        Match = match;
    }

    public string GetGroup(string group)
    {
        return Match.Groups[group].Value;
    }

    public ListValue AsList() => throw new InvalidOperationException();
    public StringValue AsString() => throw new InvalidOperationException();
    public bool IsBlank => false;

    public override string ToString() => Match.ToString();
}