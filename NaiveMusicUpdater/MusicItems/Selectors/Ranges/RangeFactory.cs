namespace NaiveMusicUpdater;

public static class RangeFactory
{
    public static Range Create(YamlNode node)
    {
        var str = node.String();
        if (str == "all")
            return new Range(null, null);
        int? single = node.Int();
        if (single != null)
            return new Range(single.Value, single.Value + 1);
        switch (node)
        {
            case YamlMappingNode:
            {
                int? start = node.Go("start").Int();
                int? stop = node.Go("stop").Int();
                return new Range(start, stop + 1);
            }
            case YamlSequenceNode seq:
            {
                int start = seq[0].Int().Value;
                int stop = seq[1].Int().Value;
                return new Range(start, stop + 1);
            }
            default:
                throw new ArgumentException($"Can't make range from {node}");
        }
    }
}

public record SafeRange(int Start, int End);

public record Range(int? Start, int? End)
{
    public SafeRange WithLength(int length)
    {
        int start = Start ?? 0;
        int end = End ?? length;
        start = Math.Min(length, start);
        end = Math.Min(length, end);
        if (start < 0)
            start = (start % length + length) % length;
        if (end < 0)
            end = (end % length + length) % length;
        return new SafeRange(start, end);
    }
}