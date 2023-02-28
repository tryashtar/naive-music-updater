using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace NaiveMusicUpdater;

public static class ArtCache
{
    public static readonly Dictionary<string, IPicture> Cached = new();
    public static IPicture? GetPicture(string path)
    {
        if (Cached.TryGetValue(path, out var result))
            return result;
        if (!File.Exists(path))
            return null;
        var art = new Picture(path);
        Cached[path] = art;
        return art;
    }
}
