namespace NaiveMusicUpdater;

public class IndexOperator : IValueOperator
{
    public readonly Range Range;
    public readonly OutofBoundsDecision OutOfBounds;
    public readonly int? MinLength;

    public IndexOperator(Range range, OutofBoundsDecision oob, int? min_length = null)
    {
        Range = range;
        OutOfBounds = oob;
        MinLength = min_length;
    }

    public IValue? Apply(IMusicItem item, IValue original)
    {
        var list = original.AsList();
        if (MinLength != null && list.Values.Count < MinLength)
            return null;
        var items = RangeFactory.Get(list.Values.ToArray(), Range, OutOfBounds);
        if (items == null)
            return null;
        return items.Length == 1 ? new StringValue(items[0]) : new ListValue(items);
    }
}

public enum OutofBoundsDecision
{
    Exit,
    Wrap,
    Clamp
}