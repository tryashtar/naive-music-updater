namespace NaiveMusicUpdater;

public class SplitOperator : IValueOperator
{
    public readonly string Separator;
    public readonly NoSeparatorDecision NoSeparator;

    public SplitOperator(string separator, NoSeparatorDecision decision)
    {
        Separator = separator;
        NoSeparator = decision;
    }

    public IValue? Apply(IMusicItem item, IValue original)
    {
        var text = original.AsString();

        string[] parts = text.Value.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && NoSeparator == NoSeparatorDecision.Exit)
            return null;

        return new ListValue(parts);
    }
}

public enum NoSeparatorDecision
{
    Exit,
    Ignore
}