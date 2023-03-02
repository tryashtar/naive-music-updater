using YamlDotNet.Serialization;

namespace NaiveMusicUpdater;

public class LibraryCache
{
    public readonly string Folder;
    private readonly Dictionary<string, DateTime> DateCache;
    private readonly Dictionary<string, DateTime> PendingDateCache;
    private string DateCachePath => Path.Combine(Folder, "datecache.yaml");
    public LibraryCache(string folder)
    {
        Folder = folder;
        if (File.Exists(DateCachePath))
        {
            var datecache = File.ReadAllText(DateCachePath);
            var deserializer = new DeserializerBuilder().Build();
            DateCache = deserializer.Deserialize<Dictionary<string, DateTime>>(datecache) ?? new Dictionary<string, DateTime>();
            PendingDateCache = new Dictionary<string, DateTime>(DateCache);
        }
        else
        {
            Logger.WriteLine($"Couldn't find date cache {DateCachePath}, starting fresh");
            DateCache = new Dictionary<string, DateTime>();
            PendingDateCache = new Dictionary<string, DateTime>();
        }
    }

    public void Save()
    {
        var serializer = new SerializerBuilder().Build();
        Directory.CreateDirectory(Path.GetDirectoryName(DateCachePath));
        File.WriteAllText(DateCachePath, serializer.Serialize(PendingDateCache));
    }

    public bool NeedsUpdate(IMusicItem item)
    {
        foreach (var path in RelevantPaths(item))
        {
            var date = TouchedTime(path);
            if (DateCache.TryGetValue(path, out var cached))
            {
                if (date - TimeSpan.FromSeconds(5) > cached)
                    return true;
            }
            else
                return true;
        }
        return false;
    }

    private IEnumerable<string> RelevantPaths(IMusicItem item)
    {
        yield return item.Location;
        foreach (var parent in item.PathFromRoot())
        {
            foreach (var config in parent.Configs)
            {
                yield return config.Location;
            }
        }
    }

    private static DateTime TouchedTime(string filepath)
    {
        DateTime modified = File.GetLastWriteTime(filepath);
        DateTime created = File.GetCreationTime(filepath);
        return modified > created ? modified : created;
    }
    
    public void MarkUpdatedRecently(IMusicItem item)
    {
        foreach (var path in RelevantPaths(item))
        {
            PendingDateCache[path] = DateTime.Now;
        }
    }

    public void MarkNeedsUpdateNextTime(IMusicItem item)
    {
        PendingDateCache.Remove(item.Location);
    }
}
