﻿namespace NaiveMusicUpdater;

public class MusicItemConfig : IMusicItemConfig
{
    public string Location { get; }
    private readonly ISongOrder? TrackOrder;
    private readonly ISongOrder? DiscOrder;
    private readonly IMetadataStrategy? ThisStrategy;
    private readonly IMetadataStrategy? SongsStrategy;
    private readonly IMetadataStrategy? FoldersStrategy;
    private readonly List<TargetedStrategy> MetadataStrategies;
    private readonly List<TargetedStrategy> SharedStrategies;
    private readonly IMusicItem ConfiguredItem;

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
        SharedStrategies = yaml.Go("set all").ToList(x => ParseMultiple(x.Go("names"), x.Go("set"))) ?? new();
    }

    public YamlNode Serialize()
    {
        var node = new YamlMappingNode();
        return node;
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
        return new TargetedStrategy(new MultiItemSelector(selectors), strategy);
    }

    private IMetadataStrategy LiteralOrReference(YamlNode node)
    {
        if (node is YamlScalarNode scalar && scalar.Value != null)
            return ConfiguredItem.GlobalConfig.GetNamedStrategy(scalar.Value);
        else
        {
            if (node is YamlSequenceNode sequence)
                return new MultipleStrategy(sequence.Select(LiteralOrReference));
            else
                return MetadataStrategyFactory.Create(node);
        }
    }

    public Metadata GetMetadata(IMusicItem item, Predicate<MetadataField> desired)
    {
        var metadata = new Metadata();
        if (item == ConfiguredItem && ThisStrategy != null)
            metadata.MergeWith(ThisStrategy.Get(item, desired), ThisStrategy.Mode);
        if (item is MusicFolder)
        {
            if (FoldersStrategy != null)
                metadata.MergeWith(FoldersStrategy.Get(item, desired), FoldersStrategy.Mode);
        }

        if (item is Song)
        {
            if (SongsStrategy != null)
                metadata.MergeWith(SongsStrategy.Get(item, desired), SongsStrategy.Mode);
            if (DiscOrder != null)
                metadata.MergeWith(DiscOrder.Get(item), CombineMode.Replace);
            if (TrackOrder != null)
                metadata.MergeWith(TrackOrder.Get(item), CombineMode.Replace);
        }

        foreach (var strat in SharedStrategies.Concat(MetadataStrategies))
        {
            if (strat.Selector.IsSelectedFrom(ConfiguredItem, item))
                metadata.MergeWith(strat.Strategy.Get(item, desired), strat.Strategy.Mode);
        }

        return metadata;
    }

    public CheckSelectorResults CheckSelectors()
    {
        var results = new CheckSelectorResults();
        IEnumerable<IItemSelector> all_selectors = SharedStrategies.Concat(MetadataStrategies).Select(x => x.Selector);
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