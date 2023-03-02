using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NaiveMusicUpdater;

public class ArtRepo
{
    public readonly string Folder;
    private IArtCache Cache;
    private Dictionary<string, ArtConfig> ConfigCache = new();

    public ArtRepo(string folder, IArtCache cache)
    {
        Folder = folder;
        Cache = cache;
    }

    public IPicture? GetProcessed(string path)
    {
        var cached = Cache.Get(path);
        if (cached != null)
            return cached;
        var template = LoadTemplate(path);
        if (template == null)
            return null;
        var settings = GetSettings(path);
        var result = ProcessTemplate(template, settings);
        Cache.Put(path, result);
        return result;
    }

    private IPicture ProcessTemplate(Image<Rgba32> image, ProcessArtSettings settings)
    {
        image.Mutate(x =>
        {
            if (settings.Background != null)
                x.BackgroundColor(settings.Background.Value);
        });
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return new Picture(stream.ToArray());
    }

    private ProcessArtSettings GetSettings(string path)
    {
        ProcessArtSettings settings = new ProcessArtSettings();
        foreach (var config in GetConfigs(path))
        {
            foreach (var (check, apply) in config.Settings)
            {
                if (check(path))
                    settings.MergeWith(apply);
            }
        }

        return settings;
    }

    private IEnumerable<ArtConfig> GetConfigs(string path)
    {
        while (path != "")
        {
            if (ConfigCache.TryGetValue(path, out var existing))
                yield return existing;
            var config = new ArtConfig(Folder, path);
            ConfigCache[path] = config;
            yield return config;
            path = Path.GetDirectoryName(path);
        }
    }

    private Image<Rgba32>? LoadTemplate(string path)
    {
        var name = Path.Combine(Folder, path);
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(name)))
        {
            if (Path.GetFileNameWithoutExtension(file) == Path.GetFileName(name))
                return Image.Load<Rgba32>(file);
        }

        return null;
    }
}