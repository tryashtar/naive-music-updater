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
        foreach (var item in ArtCache.Cached.Keys)
        {
            PendingDateCache[item] = DateTime.Now;
        }
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
        var art = GetArtPathFor(item);
        if (art != null)
            yield return art;
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

    private static readonly Regex NonAscii = new(@"[^\u0000-\u007F]+");
    public string? GetArtPathFor(IMusicItem? item)
    {
        while (item != null)
        {
            var partial = Util.StringPathAfterRoot(item);
            partial = NonAscii.Replace(partial, "_");
            var path = Path.Combine(Folder, "art", partial + ".png");
            if (File.Exists(path))
                return path;
            if (item is Song)
            {
                string parent = Path.GetDirectoryName(partial)!;
                string contents = Path.Combine(parent, "__contents__");
                var contents_path = Path.Combine(Folder, "art", contents + ".png");
                if (File.Exists(contents_path))
                    return contents_path;
            }
            item = item.Parent;
        }
        return null;
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
