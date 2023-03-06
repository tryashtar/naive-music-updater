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
        var values = items.Select(Getter.Get).Where(x => x != null).ToArray();
        if (values.Length == 0)
            return null;
        var result = values.Length switch
        {
            1 => values[0],
            _ => new ListValue(values.Select(x => x.AsString().Value))
        };
        return Modifier == null ? result : Modifier.Apply(item, result);
    }
}