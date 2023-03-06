namespace NaiveMusicUpdater;

public class JoinOperator : IValueOperator
{
    public readonly IValueSource Value;

    public JoinOperator(IValueSource value)
    {
        Value = value;
    }

    public IValue? Apply(IMusicItem item, IValue original)
    {
        var list = original.AsList().Values;
        var middle = Value.Get(item);
        return middle == null ? null : new StringValue(String.Join(middle.AsString().Value, list));
    }
}