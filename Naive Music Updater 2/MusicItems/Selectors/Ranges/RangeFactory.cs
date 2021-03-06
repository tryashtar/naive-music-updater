namespace NaiveMusicUpdater;

public static class RangeFactory
{
    public static Range Create(YamlNode node)
    {
        int? single = node.Int();
        if (single != null)
            return new Range(single.Value, single.Value + 1);
        if (node is YamlMappingNode)
        {
            int start = node.Go("start").Int() ?? 0;
            int stop = node.Go("stop").Int() ?? int.MaxValue - 1;
            return new Range(start, stop >= 0 ? stop + 1 : stop);
        }
        throw new ArgumentException($"Can't make range from {node}");
    }

}

public record Range(int Start, int End)
{
    public Range WithLength(int length)
    {
        int end = End >= 0 ? End : length + End;
        end = Math.Clamp(end, 1, length);
        int start = Start >= length ? end : Start;
        return new Range(start, end);
    }
}
