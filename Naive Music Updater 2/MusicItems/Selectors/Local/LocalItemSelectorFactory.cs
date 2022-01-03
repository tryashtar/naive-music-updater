namespace NaiveMusicUpdater;

public interface ILocalItemSelector : IItemSelector
{ }

public static class LocalItemSelectorFactory
{
    public static ILocalItemSelector Create(YamlNode node)
    {
        if (node is YamlScalarNode scalar)
        {
            var type = scalar.ToEnum<SimpleSource>();
            if (type == SimpleSource.This || type == SimpleSource.Self)
                return ThisItemSelector.Instance;
        }
        else if (node is YamlMappingNode map)
        {
            var must = map.Go("must_be").ToEnum<MusicItemType>();
            var up = map.Go("up");
            if (up != null)
                return new DrillingItemSelector(DrillDirection.Up, up.Parse(RangeFactory.Create), must);
            var down = map.Go("from_root");
            if (down != null)
                return new DrillingItemSelector(DrillDirection.Down, down.Parse(RangeFactory.Create), must);
            var selector = map.Go("selector").NullableParse(ItemSelectorFactory.Create);
            if (selector != null)
                return new LocalSelectorWrapper(selector);
        }
        throw new ArgumentException($"Can't make local item selector from {node}");
    }
}

public enum SimpleSource
{
    This,
    Self
}

public enum MusicItemType
{
    File,
    Folder
}
