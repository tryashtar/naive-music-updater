namespace NaiveMusicUpdater;

public class MusicItemSource : IValueSource
{
    public readonly ILocalItemSelector Selector;
    public readonly IMusicItemValueSource Getter;
    public readonly IValueOperator Modifier;

    public MusicItemSource(ILocalItemSelector selector, IMusicItemValueSource getter, IValueOperator modifier)
    {
        Selector = selector;
        Getter = getter;
        Modifier = modifier;
    }

    public IValue Get(IMusicItem item)
    {
        var items = Selector.AllMatchesFrom(item);
        if (!items.Any())
            return BlankValue.Instance;
        var values = items.Select(GetAndModify).ToArray();
        if (values.Length == 1)
            return values[0];
        return new ListValue(values.Select(x => x.AsString().Value));
    }

    private IValue GetAndModify(IMusicItem item)
    {
        var value = Getter.Get(item);
        if (Modifier != null)
            value = Modifier.Apply(item, value);
        return value;
    }
}
