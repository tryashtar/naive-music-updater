namespace NaiveMusicUpdater;

public class AppendOperator : IValueOperator
{
    public readonly IValueSource Value;
    public readonly AppendMode Mode;
    public readonly Range? Range;

    public AppendOperator(IValueSource text, AppendMode mode, Range? range)
    {
        Value = text;
        Mode = mode;
        Range = range;
    }

    public IValue? Apply(IMusicItem item, IValue original)
    {
        var extra = Value.Get(item);
        if (extra == null)
            return null;

        if (Range == null)
        {
            var text = original.AsString();
            string val = Modify(text.Value, extra.AsString().Value);
            return new StringValue(val);
        }
        else
        {
            var list = original.AsList().Values.ToArray();
            var indices = RangeFactory.GetIndices(list, Range.Value, OutofBoundsDecision.Exit);
            if (indices == null)
                return null;
            for (int i = indices.Value.start; i < indices.Value.end; i++)
            {
                list[i] = Modify(list[i], extra.AsString().Value);
            }

            return new ListValue(list);
        }
    }

    private string Modify(string val, string extra)
    {
        return Mode == AppendMode.Append ? val + extra : extra + val;
    }
}

public enum AppendMode
{
    Append,
    Prepend
}