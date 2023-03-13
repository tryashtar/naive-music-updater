using YamlDotNet.Serialization;

namespace NaiveMusicUpdater;

public interface IFileDateCache
{
    void Save();
    bool ChangedSinceLastRun(string path);
    void Acknowledge(string path);
    bool IsAcknowledged(string path);
}

public static class FileDateCacheExtensions
{
    public static bool NeedsUpdate(this IFileDateCache cache, IMusicItem item)
    {
        if (cache.IsAcknowledged(item.Location))
            return false;
        return RelevantPaths(item).Any(cache.ChangedSinceLastRun);
    }

    public static void MarkUpdated(this IFileDateCache cache, IMusicItem item)
    {
        foreach (var path in RelevantPaths(item))
        {
            cache.Acknowledge(path);
        }
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

        if (item.RootLibrary.LibraryConfig.ArtTemplates != null)
        {
            var art = item.GetMetadata(MetadataField.Art.Only).Get(MetadataField.Art);
            if (!art.IsBlank)
            {
                foreach (var val in art.AsList().Values)
                {
                    var relevant = item.RootLibrary.LibraryConfig.ArtTemplates.RelevantPaths(val);
                    foreach (var r in relevant)
                    {
                        yield return r;
                    }
                }
            }
        }
    }
}

public class FileDateCache : IFileDateCache
{
    public readonly string FilePath;
    private readonly Dictionary<string, DateTime> DateCache;
    private readonly HashSet<string> Acknowledged = new();

    public FileDateCache(string file)
    {
        FilePath = file;
        if (File.Exists(file))
        {
            var deserializer = new DeserializerBuilder().Build();
            DateCache = deserializer.Deserialize<Dictionary<string, DateTime>>(File.OpenText(file)) ?? new();
        }
        else
        {
            Logger.WriteLine($"Couldn't find date cache {file}, starting fresh", ConsoleColor.Yellow);
            DateCache = new();
        }
    }

    public void Save()
    {
        foreach (var item in Acknowledged)
        {
            DateCache[item] = DateTime.Now;
        }

        var serializer = new SerializerBuilder().Build();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, serializer.Serialize(DateCache));
    }

    public bool ChangedSinceLastRun(string path)
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

    public void Acknowledge(string path)
    {
        Acknowledged.Add(path);
    }

    public bool IsAcknowledged(string path)
    {
        return Acknowledged.Contains(path);
    }
}

public class MemoryFileDateCache : IFileDateCache
{
    public HashSet<string> Updated = new();

    public void Save()
    {
    }

    public virtual bool ChangedSinceLastRun(string path)
    {
        return !Updated.Contains(path);
    }

    public void Acknowledge(string path)
    {
        Updated.Add(path);
    }

    public bool IsAcknowledged(string path)
    {
        return Updated.Contains(path);
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

    public override bool ChangedSinceLastRun(string path)
    {
        if (Path.GetExtension(path) == ".png")
            return false;
        return Check.Any(x => path.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
}
#endif