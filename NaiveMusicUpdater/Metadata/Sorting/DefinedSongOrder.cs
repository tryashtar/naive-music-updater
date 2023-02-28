namespace NaiveMusicUpdater;

public class DefinedSongOrder : ISongOrder
{
    private readonly List<IMusicItem> Unselected;
    private readonly Dictionary<IMusicItem, uint> CachedResults;
    public readonly IItemSelector Order;
    public readonly uint TotalNumber;
    public ReadOnlyCollection<IMusicItem> UnselectedItems => Unselected.AsReadOnly();
    public DefinedSongOrder(IItemSelector order, MusicFolder folder)
    {
        Order = order;
        CachedResults = new Dictionary<IMusicItem, uint>();
        var used_folders = new HashSet<MusicFolder>();
        uint index = 0;
        foreach (var item in order.AllMatchesFrom(folder))
        {
            index++;
            CachedResults[item] = index;
            used_folders.Add(item.Parent);
        }
        TotalNumber = index;
        Unselected = new List<IMusicItem>();
        foreach (var used in used_folders)
        {
            Unselected.AddRange(used.Songs.Except(CachedResults.Keys));
        }
    }

    public uint? GetTrack(IMusicItem item)
    {
        if (CachedResults.TryGetValue(item, out uint result))
            return result;
        return null;
    }

    public Metadata Get(IMusicItem item)
    {
        var metadata = new Metadata();
        if (CachedResults.TryGetValue(item, out uint track))
        {
            metadata.Register(MetadataField.Track, new MetadataProperty(new NumberValue(track), CombineMode.Replace));
            metadata.Register(MetadataField.TrackTotal, new MetadataProperty(new NumberValue(TotalNumber), CombineMode.Replace));
        }
        return metadata;
    }
}
