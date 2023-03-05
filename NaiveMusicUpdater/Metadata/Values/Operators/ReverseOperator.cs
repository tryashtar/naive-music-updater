namespace NaiveMusicUpdater;

public class ReverseOperator : IValueOperator
{
    public static readonly ReverseOperator Instance = new();
    public IValue? Apply(IMusicItem item, IValue original)
    {
        var list = original.AsList();
        var new_list = list.Values.ToList();
        new_list.Reverse();
        return new ListValue(new_list);
    }
}