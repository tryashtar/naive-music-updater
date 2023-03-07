using YamlDotNet.Serialization;

namespace NaiveMusicUpdater;

public interface IFileDateCache
{
    void Save();
    bool NeedsUpdate(string path);
    void MarkUpdated(string path);
    void MarkPendingUpdated(string path);
    void MarkPendingNotUpdated(string path);
}

public static class FileDateCacheExtensions
{
    public static bool NeedsUpdate(this IFileDateCache cache, IMusicItem item)
    {
        return RelevantPaths(item).Any(cache.NeedsUpdate);
    }

    public static void MarkUpdated(this IFileDateCache cache, IMusicItem item)
    {
        cache.MarkUpdated(item.Location);
        foreach (var path in RelevantPaths(item))
        {
            cache.MarkPendingUpdated(path);
        }
    }

    public static void MarkPendingNotUpdated(this IFileDateCache cache, IMusicItem item)
    {
        cache.MarkPendingNotUpdated(item.Location);
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
                    var path = item.RootLibrary.LibraryConfig.ArtTemplates.GetTemplatePath(val);
                    if (path != null)
                    {
                        yield return path;
                        foreach (var conf in item.RootLibrary.LibraryConfig.ArtTemplates.GetConfigPaths(val))
                        {
                            yield return conf;
                        }
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
    private readonly Dictionary<string, DateTime> PendingDateCache;

    public FileDateCache(string file)
    {
        FilePath = file;
        if (File.Exists(file))
        {
            var deserializer = new DeserializerBuilder().Build();
            DateCache = deserializer.Deserialize<Dictionary<string, DateTime>>(File.OpenText(file)) ?? new();
            PendingDateCache = new(DateCache);
        }
        else
        {
            Logger.WriteLine($"Couldn't find date cache {file}, starting fresh", ConsoleColor.Yellow);
            DateCache = new();
            PendingDateCache = new();
        }
    }

    public void Save()
    {
        var serializer = new SerializerBuilder().Build();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, serializer.Serialize(PendingDateCache));
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

    public void MarkUpdated(string path)
    {
        DateCache[path] = DateTime.Now;
        PendingDateCache[path] = DateTime.Now;
    }

    public void MarkPendingUpdated(string path)
    {
        PendingDateCache[path] = DateTime.Now;
    }

    public void MarkPendingNotUpdated(string path)
    {
        PendingDateCache.Remove(path);
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

    public void MarkUpdated(string path)
    {
        Updated.Add(path);
    }

    public void MarkPendingUpdated(string path)
    {
    }

    public void MarkPendingNotUpdated(string path)
    {
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
        return Check.Any(x => path.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
}
#endif