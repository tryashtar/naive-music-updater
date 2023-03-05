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

    public void Apply(Metadata start, IMusicItem item)
    {
        if (CachedResults.TryGetValue(item, out uint track))
        {
            start.Register(MetadataField.Track, new NumberValue(track));
            start.Register(MetadataField.TrackTotal, new NumberValue(TotalNumber));
        }
    }
}