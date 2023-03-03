namespace NaiveMusicUpdater;

public class MusicItemSource : IValueSource
{
    public readonly ILocalItemSelector Selector;
    public readonly IMusicItemValueSource Getter;
    public readonly IValueOperator? Modifier;

    public MusicItemSource(ILocalItemSelector selector, IMusicItemValueSource getter, IValueOperator? modifier)
    {
        Selector = selector;
        Getter = getter;
        Modifier = modifier;
    }

    public IValue? Get(IMusicItem item)
    {
        var items = Selector.AllMatchesFrom(item);
        if (!items.Any())
            return null;
        var values = items.Select(GetAndModify).Where(x => x != null).ToArray();
        return values.Length switch
        {
            0 => null,
            1 => values[0],
            _ => new ListValue(values.Select(x => x.AsString().Value))
        };
    }

    private IValue? GetAndModify(IMusicItem item)
    {
        var value = Getter.Get(item);
        if (Modifier != null)
            value = Modifier.Apply(item, value);
        return value;
    }
}