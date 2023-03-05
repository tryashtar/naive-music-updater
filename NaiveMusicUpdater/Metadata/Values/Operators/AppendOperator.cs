namespace NaiveMusicUpdater;

public class AppendOperator : IValueOperator
{
    public readonly IValueSource Value;
    public readonly AppendMode Mode;

    public AppendOperator(IValueSource text, AppendMode mode)
    {
        Value = text;
        Mode = mode;
    }

    public IValue? Apply(IMusicItem item, IValue original)
    {
        var text = original.AsString();
        var extra = Value.Get(item);
        if (extra == null)
            return null;

        string val = Mode == AppendMode.Append
            ? text.Value + extra.AsString().Value
            : extra.AsString().Value + text.Value;
        return new StringValue(val);
    }
}

public enum AppendMode
{
    Append,
    Prepend
}