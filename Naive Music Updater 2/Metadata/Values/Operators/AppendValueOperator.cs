namespace NaiveMusicUpdater;

public class AppendValueOperator : IValueOperator
{
    public readonly IValueSource Value;
    public readonly AppendMode Mode;
    public AppendValueOperator(IValueSource text, AppendMode mode)
    {
        Value = text;
        Mode = mode;
    }

    public IValue Apply(IMusicItem item, IValue original)
    {
        if (original.IsBlank)
            return BlankValue.Instance;

        var text = original.AsString();
        var extra = Value.Get(item).AsString();

        string val = Mode == AppendMode.Append ? text.Value + extra.Value : extra.Value + text.Value;
        return new StringValue(val);
    }
}

public enum AppendMode
{
    Append,
    Prepend
}
