using YamlDotNet.Serialization;

namespace NaiveMusicUpdater;

public interface IFileDateCache
{
    void Save();
    bool NeedsUpdate(string path);
    void MarkUpdatedRecently(string path);
    void MarkNeedsUpdateNextTime(string path);
}

public static class FileDateCacheExtensions
{
    public static bool NeedsUpdate(this IFileDateCache cache, IMusicItem item)
    {
        return RelevantPaths(item).Any(cache.NeedsUpdate);
    }

    public static void MarkUpdatedRecently(this IFileDateCache cache, IMusicItem item)
    {
        foreach (var path in RelevantPaths(item))
        {
            cache.MarkUpdatedRecently(path);
        }
    }

    public static void MarkNeedsUpdateNextTime(this IFileDateCache cache, IMusicItem item)
    {
        cache.MarkNeedsUpdateNextTime(item.Location);
    }

    private static IEnumerable<string> RelevantPaths(IMusicItem item)
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
}

public class FileDateCache : IFileDateCache
{
    public readonly string FilePath;
    private readonly Dictionary<string, DateTime> DateCache;

    public FileDateCache(string file)
    {
        FilePath = file;
        if (File.Exists(file))
        {
            var deserializer = new DeserializerBuilder().Build();
            DateCache = deserializer.Deserialize<Dictionary<string, DateTime>>(File.OpenText(file)) ??
                        new Dictionary<string, DateTime>();
        }
        else
        {
            Logger.WriteLine($"Couldn't find date cache {file}, starting fresh", ConsoleColor.Yellow);
            DateCache = new Dictionary<string, DateTime>();
        }
    }

    public void Save()
    {
        var serializer = new SerializerBuilder().Build();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, serializer.Serialize(DateCache));
    }

    public bool NeedsUpdate(string path)
    {
        var date = TouchedTime(path);
        if (DateCache.TryGetValue(path, out var cached))
            return date - TimeSpan.FromSeconds(5) > cached;
        return true;
    }

    private static DateTime TouchedTime(string filepath)
    {
        var modified = File.GetLastWriteTime(filepath);
        var created = File.GetCreationTime(filepath);
        return modified > created ? modified : created;
    }

    public void MarkUpdatedRecently(string path)
    {
        DateCache[path] = DateTime.Now;
    }

    public void MarkNeedsUpdateNextTime(string path)
    {
        DateCache.Remove(path);
    }
}

public class MemoryFileDateCache : IFileDateCache
{
    public HashSet<string> Updated = new();

    public void Save()
    {
    }

    public virtual bool NeedsUpdate(string path)
    {
        return !Updated.Contains(path);
    }

    public void MarkUpdatedRecently(string path)
    {
        Updated.Add(path);
    }

    public void MarkNeedsUpdateNextTime(string path)
    {
        Updated.Remove(path);
    }
}

#if DEBUG
public class DebugFileDateCache : MemoryFileDateCache
{
    public readonly List<string> Check;

    public DebugFileDateCache(List<string> check)
    {
        Check = check;
    }

    public override bool NeedsUpdate(string path)
    {
        if (Path.GetExtension(path) == ".png")
            return false;
        return Check.Any(x => path.Contains(x, StringComparison.OrdinalIgnoreCase)) || base.NeedsUpdate(path);
    }
}
#endif