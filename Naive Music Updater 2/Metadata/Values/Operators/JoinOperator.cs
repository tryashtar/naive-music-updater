namespace NaiveMusicUpdater;

public class JoinOperator : IValueOperator
{
    public readonly IValueSource Value;
    public JoinOperator(IValueSource value)
    {
        Value = value;
    }

    public IValue Apply(IMusicItem item, IValue original)
    {
        if (original.IsBlank)
            return BlankValue.Instance;

        var list = original.AsList().Values;
        var middle = Value.Get(item).AsString().Value;

        return new StringValue(String.Join(middle, list));
    }
}

