using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace NaiveMusicUpdater;

public class ArtRepo
{
    public readonly Dictionary<string, ProcessArtSettings> NamedSettings;
    public readonly string Folder;
    private readonly IArtCache Cache;
    private readonly IFileDateCache DateCache;
    private readonly string? IcoFolder;
    private readonly Dictionary<string, ArtConfig> ConfigCache = new();

    public ArtRepo(string folder, IArtCache cache, IFileDateCache datecache, string? ico_folder,
        Dictionary<string, ProcessArtSettings> named_settings)
    {
        Folder = folder;
        Cache = cache;
        DateCache = datecache;
        IcoFolder = ico_folder;
        NamedSettings = named_settings;
    }

    private static void SaveIcon(Image<Rgba32> image, Stream stream)
    {
        image.Mutate(x =>
        {
            x.Resize(new ResizeOptions()
            {
                Size = new(256, 256),
                Mode = ResizeMode.Pad
            });
        });
        using var data_stream = new MemoryStream();
        image.SaveAsPng(data_stream);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write((byte)image.Width);
        writer.Write((byte)image.Height);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((short)0);
        writer.Write((short)32);
        writer.Write((int)data_stream.Length);
        writer.Write((int)(6 + 16));
        data_stream.WriteTo(stream);
        writer.Flush();
    }

    public (IPicture? picture, string? path) FirstArt(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var pic = GetProcessed(path);
            if (pic != null)
                return (pic, path);
        }

        return (null, null);
    }

    public string? GetIcon(string path)
    {
        if (IcoFolder == null)
            return null;
        var template_path = GetTemplatePath(path);
        if (template_path == null)
            return null;
        var ico_path = Path.Combine(IcoFolder, DiskArtCache.NonAscii.Replace(path, "_")) + ".ico";
        if (!DateCache.ChangedSinceLastRun(template_path) && File.Exists(ico_path))
            return ico_path;
        var template = Image.Load<Rgba32>(template_path);
        var settings = GetSettings(path);
        settings.Apply(template);
        using var stream = File.Create(ico_path);
        SaveIcon(template, stream);
        DateCache.Acknowledge(template_path);
        return ico_path;
    }

    public IEnumerable<string> RelevantPaths(string path)
    {
        var template = GetTemplatePath(path);
        if (template != null)
        {
            yield return template;
            foreach (var conf in GetConfigPaths(path))
            {
                yield return conf;
            }
        }
    }

    private bool NeedsUpdate(string path)
    {
        var template_path = GetTemplatePath(path);
        if (template_path == null || DateCache.IsAcknowledged(template_path))
            return false;
        return RelevantPaths(path).Any(DateCache.ChangedSinceLastRun);
    }

    private void MarkUpdated(string path)
    {
        foreach (var relevant in RelevantPaths(path))
        {
            DateCache.Acknowledge(relevant);
        }
    }

    public IPicture? GetProcessed(string path)
    {
        var template_path = GetTemplatePath(path);
        if (template_path == null)
            return null;
        if (!NeedsUpdate(path))
        {
            var cached = Cache.Get(path);
            if (cached != null)
                return cached;
        }

        var template = Image.Load<Rgba32>(template_path);
        var settings = GetSettings(path);
        settings.Apply(template);
        using var stream = new MemoryStream();
        template.SaveAsPng(stream, new PngEncoder() { TransparentColorMode = PngTransparentColorMode.Preserve });
        string name = Path.GetFileName(path + ".png");
        var result = new Picture(stream.ToArray())
        {
            Filename = name,
            Description = name
        };
        Cache.Put(path, result);
        MarkUpdated(path);
        return result;
    }

    private ProcessArtSettings GetSettings(string path)
    {
        var settings = new ProcessArtSettings();
        foreach (var config in GetConfigs(path).Reverse())
        {
            foreach (var (check, apply) in config.Settings)
            {
                if (check(path))
                    settings.MergeWith(apply);
            }
        }

        return settings;
    }

    public IEnumerable<string> GetConfigPaths(string path)
    {
        while (path != "")
        {
            path = Path.GetDirectoryName(path)!;
            var file = Path.Combine(Folder, path, "images.yaml");
            if (File.Exists(file))
                yield return file;
        }
    }

    // in order from most to least specific
    private IEnumerable<ArtConfig> GetConfigs(string path)
    {
        while (path != "")
        {
            path = Path.GetDirectoryName(path)!;
            if (ConfigCache.TryGetValue(path, out var existing))
                yield return existing;
            if (File.Exists(Path.Combine(Folder, path, "images.yaml")))
            {
                var config = new ArtConfig(this, Folder, path);
                ConfigCache[path] = config;
                yield return config;
            }
        }
    }

    public string? GetTemplatePath(string path)
    {
        var name = Path.Combine(Folder, path);
        if (!Directory.Exists(Path.GetDirectoryName(name)))
            return null;
        string? parent = Path.GetDirectoryName(name);
        if (parent == null)
            return null;
        foreach (var file in Directory.EnumerateFiles(parent))
        {
            if (Path.GetFileNameWithoutExtension(file) == Path.GetFileName(name))
                return file;
        }

        return null;
    }
}