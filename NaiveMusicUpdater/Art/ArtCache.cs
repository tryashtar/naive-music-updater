namespace NaiveMusicUpdater;

public interface IArtCache
{
    public IPicture? Get(string path);
    public void Put(string path, IPicture picture);
}

public class DiskArtCache : IArtCache
{
    public readonly string Folder;
    private readonly MemoryArtCache MemoryCache = new();

    public DiskArtCache(string folder)
    {
        Folder = folder;
    }

    public IPicture? Get(string path)
    {
        var cached = MemoryCache.Get(path);
        if (cached != null)
            return cached;
        var file = ExpandPath(path);
        if (!File.Exists(file))
            return null;
        var pic = new Picture(file);
        MemoryCache.Put(path, pic);
        return pic;
    }

    public static readonly Regex NonAscii = new(@"[^\u0000-\u007F]+");

    private string ExpandPath(string path)
    {
        return Path.Combine(Folder, NonAscii.Replace(path, "_")) + ".png";
    }

    public void Put(string path, IPicture picture)
    {
        MemoryCache.Put(path, picture);
        var file = ExpandPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        File.WriteAllBytes(file, picture.Data.Data);
    }
}

public class MemoryArtCache : IArtCache
{
    private readonly Dictionary<string, IPicture> MemoryCache = new();

    public IPicture? Get(string path)
    {
        if (MemoryCache.TryGetValue(path, out var result))
            return result;
        return null;
    }

    public void Put(string path, IPicture picture)
    {
        MemoryCache[path] = picture;
    }
}