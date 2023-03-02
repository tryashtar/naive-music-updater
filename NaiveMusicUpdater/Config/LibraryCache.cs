using YamlDotNet.Serialization;

namespace NaiveMusicUpdater;

public interface ILibraryCache
{
    void Save();
    bool NeedsUpdate(IMusicItem item);
    void MarkUpdatedRecently(IMusicItem item);
    void MarkNeedsUpdateNextTime(IMusicItem item);
}

public class FileLibraryCache : ILibraryCache
{
    public readonly string FilePath;
    private readonly Dictionary<string, DateTime> DateCache;
    private readonly Dictionary<string, DateTime> PendingDateCache;

    public FileLibraryCache(string file)
    {
        FilePath = file;
        if (File.Exists(file))
        {
            var datecache = File.ReadAllText(file);
            var deserializer = new DeserializerBuilder().Build();
            DateCache = deserializer.Deserialize<Dictionary<string, DateTime>>(datecache) ??
                        new Dictionary<string, DateTime>();
            PendingDateCache = new Dictionary<string, DateTime>(DateCache);
        }
        else
        {
            Logger.WriteLine($"Couldn't find date cache {file}, starting fresh");
            DateCache = new Dictionary<string, DateTime>();
            PendingDateCache = new Dictionary<string, DateTime>();
        }
    }

    public void Save()
    {
        var serializer = new SerializerBuilder().Build();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, serializer.Serialize(PendingDateCache));
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

public class DummyLibraryCache : ILibraryCache
{
    public void Save()
    {
    }

    public bool NeedsUpdate(IMusicItem item)
    {
        return true;
    }

    public void MarkUpdatedRecently(IMusicItem item)
    {
    }

    public void MarkNeedsUpdateNextTime(IMusicItem item)
    {
    }
}