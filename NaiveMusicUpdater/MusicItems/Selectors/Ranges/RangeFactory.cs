namespace NaiveMusicUpdater;

public static class RangeFactory
{
    public static Range Create(YamlNode node)
    {
        var str = node.String();
        if (str == "first")
            return new Range(Index.Start, Index.FromStart(1));
        if (str == "last")
            return Range.StartAt(Index.FromEnd(1));
        if (str == "all")
            return Range.All;
        int? single = node.Int();
        if (single != null)
            return new Range(Convert(single.Value), Convert(single.Value, true));
        switch (node)
        {
            case YamlMappingNode:
            {
                int? start = node.Go("start").Int();
                int? stop = node.Go("stop").Int();
                if (start != null && stop != null)
                    return new Range(Convert(start.Value), Convert(stop.Value, true));
                if (start == null && stop != null)
                    return Range.EndAt(Convert(stop.Value, true));
                if (start != null && stop == null)
                    return Range.StartAt(Convert(start.Value));
                break;
            }
            case YamlSequenceNode seq:
            {
                int start = seq[0].Int() ?? 0;
                int stop = seq[1].Int() ?? 0;
                return new Range(Convert(start), Convert(stop, true));
            }
        }

        throw new ArgumentException($"Can't make range from {node}");
    }

    private static Index Convert(int index, bool fix_exclusive = false)
    {
        if (index >= 0)
            return Index.FromStart(index + (fix_exclusive ? 1 : 0));
        return Index.FromEnd(-index + 1 - (fix_exclusive ? 1 : 0));
    }

    public static (int start, int end)? GetIndices<T>(T[] arr, Range range, OutofBoundsDecision decision)
    {
        Index start = range.Start;
        int num1 = !start.IsFromEnd ? start.Value : arr.Length - start.Value;
        Index end = range.End;
        int num2 = !end.IsFromEnd ? end.Value : arr.Length - end.Value;
        if ((uint)num2 > (uint)arr.Length)
        {
            if (decision == OutofBoundsDecision.Exit)
                return null;
            else if (decision == OutofBoundsDecision.Wrap)
                num2 = (int)((uint)num2 % arr.Length);
            else if (decision == OutofBoundsDecision.Clamp)
                num2 = (int)Math.Min((uint)num2, arr.Length - 1);
        }

        if ((uint)num1 >= (uint)num2)
            return null;
        return (num1, num2);
    }

    public static T[]? Get<T>(T[] arr, Range range, OutofBoundsDecision decision)
    {
        var indices = GetIndices(arr, range, decision);
        if (indices == null)
            return null;
        return arr[indices.Value.start..indices.Value.end];
    }
}