﻿namespace NaiveMusicUpdater;

public class DefinedDiscOrder : ISongOrder
{
    public readonly uint TotalDiscs;
    private readonly IReadOnlyDictionary<uint, DefinedSongOrder> Discs;
    public DefinedDiscOrder(IReadOnlyDictionary<uint, IItemSelector> discs, MusicFolder folder)
    {
        Discs = discs.ToDictionary(x => x.Key, x => new DefinedSongOrder(x.Value, folder));
        TotalDiscs = discs.Keys.Max();
    }

    public IEnumerable<IItemSelector> GetSelectors()
    {
        return Discs.Values.Select(x => x.Order);
    }

    public IEnumerable<IMusicItem> GetUnselectedItems()
    {
        var order = Discs.Values.ToList();
        var unselected = new HashSet<IMusicItem>(order[0].UnselectedItems);
        for (int i = 1; i < order.Count; i++)
        {
            unselected.IntersectWith(order[i].UnselectedItems);
        }
        return unselected;
    }

    public Metadata Get(IMusicItem item)
    {
        var metadata = new Metadata();
        foreach (var disc in Discs)
        {
            uint? track = disc.Value.GetTrack(item);
            if (track != null)
            {
                metadata.MergeWith(disc.Value.Get(item), CombineMode.Replace);
                metadata.Register(MetadataField.Disc, new NumberValue(disc.Key));
                metadata.Register(MetadataField.DiscTotal, new NumberValue(TotalDiscs));
            }
        }
        return metadata;
    }
}
