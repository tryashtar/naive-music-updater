namespace NaiveMusicUpdater;

public class MusicItemConfig : IMusicItemConfig
{
    public string Location { get; }
    private readonly IMusicItem ConfiguredItem;
    private readonly ISongOrder? TrackOrder;
    private readonly ISongOrder? DiscOrder;
    private readonly IMetadataStrategy? ThisStrategy;
    private readonly IMetadataStrategy? SongsStrategy;
    private readonly IMetadataStrategy? FoldersStrategy;
    private readonly List<TargetedStrategy> MetadataStrategies;
    private readonly List<TargetedStrategy> SharedStrategies;
    private readonly List<BulkSet> SetFields;

    public MusicItemConfig(string file, IMusicItem configured_item, YamlNode yaml)
    {
        Location = file;
        ConfiguredItem = configured_item;
        if (configured_item is MusicFolder folder)
        {
            DiscOrder = yaml.Go("discs").NullableParse(x => DiscOrderFactory.Create(x, folder));
            if (DiscOrder == null)
                TrackOrder = yaml.Go("order").NullableParse(x => SongOrderFactory.Create(x, folder));
        }

        ThisStrategy = yaml.Go("this").NullableParse(LiteralOrReference);
        SongsStrategy = yaml.Go("songs").NullableParse(LiteralOrReference);
        FoldersStrategy = yaml.Go("folders").NullableParse(LiteralOrReference);
        MetadataStrategies = yaml.Go("set").ToList(ParseStrategy) ?? new();
        SharedStrategies = yaml.Go("set all").ToList(x => ParseMultiple(x.Go("names")!, x.Go("set")!)) ?? new();
        SetFields = yaml.Go("set fields").ToList(ParseBulkSet) ?? new();
    }

    private BulkSet ParseBulkSet(YamlNode yaml)
    {
        var field = MetadataField.FromID(yaml.Go("field").String()!);
        var dict = yaml.Go("set").ToDictionary(ItemSelectorFactory.Create, ValueSourceFactory.Create);
        var mode = yaml.Go("mode").ToEnum(CombineMode.Replace);
        return new BulkSet(field, mode, dict!);
    }

    private TargetedStrategy ParseStrategy(YamlNode key, YamlNode value)
    {
        var selector = ItemSelectorFactory.Create(key);
        var strategy = LiteralOrReference(value);
        return new TargetedStrategy(selector, strategy);
    }

    private TargetedStrategy ParseMultiple(YamlNode names, YamlNode value)
    {
        var selectors = names.ToList(ItemSelectorFactory.Create);
        var strategy = LiteralOrReference(value);
        return new TargetedStrategy(new MultiItemSelector(selectors!), strategy);
    }

    private IMetadataStrategy LiteralOrReference(YamlNode node)
    {
        return node switch
        {
            YamlScalarNode { Value: not null } scalar => ConfiguredItem.RootLibrary.LibraryConfig.GetNamedStrategy(
                scalar.Value),
            YamlSequenceNode sequence => new MultipleStrategy(sequence.Select(LiteralOrReference)),
            _ => MetadataStrategyFactory.Create(node)
        };
    }

    public void Apply(Metadata meta, IMusicItem item, Predicate<MetadataField> desired)
    {
        if (item == ConfiguredItem)
            ThisStrategy?.Apply(meta, item, desired);
        switch (item)
        {
            case MusicFolder:
            {
                if (item != ConfiguredItem)
                    FoldersStrategy?.Apply(meta, item, desired);
                break;
            }
            case Song:
            {
                SongsStrategy?.Apply(meta, item, desired);
                DiscOrder?.Apply(meta, item);
                TrackOrder?.Apply(meta, item);
                break;
            }
        }

        foreach (var bulk in SetFields)
        {
            foreach (var (select, val) in bulk.Items)
            {
                if (select.IsSelectedFrom(ConfiguredItem, item))
                {
                    var value = val.Get(item);
                    if (value != null)
                        meta.Combine(bulk.Field, value, bulk.Mode);
                }
            }
        }

        foreach (var strat in SharedStrategies.Concat(MetadataStrategies))
        {
            if (strat.Selector.IsSelectedFrom(ConfiguredItem, item))
                strat.Strategy.Apply(meta, item, desired);
        }
    }

    public CheckSelectorResults CheckSelectors()
    {
        var results = new CheckSelectorResults();
        var all_selectors = SharedStrategies.Concat(MetadataStrategies).Select(x => x.Selector)
            .Concat(SetFields.SelectMany(x => x.Items.Keys));
        if (TrackOrder is DefinedSongOrder tracks)
        {
            all_selectors = all_selectors.Append(tracks.Order);
            results.UnselectedItems.AddRange(tracks.UnselectedItems);
        }

        if (DiscOrder is DefinedDiscOrder discs)
        {
            all_selectors = all_selectors.Concat(discs.GetSelectors());
            results.UnselectedItems.AddRange(discs.GetUnselectedItems());
        }

        foreach (var selector in all_selectors)
        {
            results.UnusedSelectors.AddRange(selector.UnusedFrom(ConfiguredItem));
        }

        return results;
    }
}

public record TargetedStrategy(IItemSelector Selector, IMetadataStrategy Strategy);

public record BulkSet(MetadataField Field, CombineMode Mode, Dictionary<IItemSelector, IValueSource> Items);