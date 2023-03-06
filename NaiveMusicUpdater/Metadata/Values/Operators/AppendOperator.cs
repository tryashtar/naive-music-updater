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
            var list = original.AsList().Values.ToList();
            var range = Range.WithLength(list.Count);
            for (int i = range.Start; i < range.End; i++)
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