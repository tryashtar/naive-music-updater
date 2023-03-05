using System.Numerics;
using SixLabors.ImageSharp;
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
        if (!DateCache.NeedsUpdate(template_path) && File.Exists(ico_path))
            return ico_path;
        var template = Image.Load<Rgba32>(template_path);
        var settings = GetSettings(path);
        ProcessTemplate(template, settings);
        using var stream = File.Create(ico_path);
        SaveIcon(template, stream);
        DateCache.MarkUpdatedRecently(template_path);
        return ico_path;
    }

    public IPicture? GetProcessed(string path)
    {
        var template_path = GetTemplatePath(path);
        if (template_path == null)
            return null;
        if (!DateCache.NeedsUpdate(template_path))
        {
            var cached = Cache.Get(path);
            if (cached != null)
                return cached;
        }

        var template = Image.Load<Rgba32>(template_path);
        var settings = GetSettings(path);
        ProcessTemplate(template, settings);
        using var stream = new MemoryStream();
        template.SaveAsPng(stream);
        string name = Path.GetFileName(path + ".png");
        var result = new Picture(stream.ToArray())
        {
            Filename = name,
            Description = name
        };
        Cache.Put(path, result);
        DateCache.MarkUpdatedRecently(template_path);
        return result;
    }

    private void ProcessTemplate(Image<Rgba32> image, ProcessArtSettings settings)
    {
        image.Mutate(x =>
        {
            if (settings.HasBuffer ?? false)
            {
                var bounding = GetBoundingRectangle(x);
                x.Crop(bounding);
            }

            if (settings.Width != null || settings.Height != null)
            {
                int width = settings.Width ?? 0;
                int height = settings.Height ?? 0;
                if (settings.IntegerScale ?? false)
                {
                    if (width > 0)
                        width = width / image.Width * image.Width;
                    if (height > 0)
                        height = height / image.Height * image.Height;
                }

                if (settings.HasBuffer ?? false)
                {
                    width -= settings.Buffer[0] + settings.Buffer[2];
                    height -= settings.Buffer[1] + settings.Buffer[3];
                }

                var resize = new ResizeOptions()
                {
                    Mode = settings.Scale ?? ResizeMode.BoxPad,
                    Sampler = settings.Interpolation ?? KnownResamplers.Bicubic,
                    Size = new(width, height)
                };
                if (settings.Background != null)
                    resize.PadColor = settings.Background.Value;
                x.Resize(resize);
            }

            if (settings.HasBuffer ?? false)
            {
                var resize = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Size = new(x.GetCurrentSize().Width + settings.Buffer[2],
                        x.GetCurrentSize().Height + settings.Buffer[3]),
                    Position = AnchorPositionMode.TopLeft
                };
                if (settings.Background != null)
                    resize.PadColor = settings.Background.Value;
                x.Resize(resize);

                var resize2 = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Size = new(x.GetCurrentSize().Width + settings.Buffer[0],
                        x.GetCurrentSize().Height + settings.Buffer[1]),
                    Position = AnchorPositionMode.BottomRight
                };
                if (settings.Background != null)
                    resize2.PadColor = settings.Background.Value;
                x.Resize(resize2);
            }

            if (settings.Background != null)
                x.BackgroundColor(settings.Background.Value);
        });
    }

    private static Rectangle GetBoundingRectangle(IImageProcessingContext image)
    {
        int left = image.GetCurrentSize().Width;
        int top = image.GetCurrentSize().Height;
        int right = 0;
        int bottom = 0;
        image.ProcessPixelRowsAsVector4((row, point) =>
        {
            for (int x = 0; x < row.Length; x++)
            {
                ref var pixel = ref row[x];
                if (pixel.W != 0)
                {
                    left = Math.Min(x, left);
                    top = Math.Min(point.Y, top);
                    right = Math.Max(x, right);
                    bottom = Math.Max(point.Y, bottom);
                }
            }
        });
        return new(left, top, right - left, bottom - top);
    }

    private ProcessArtSettings GetSettings(string path)
    {
        var settings = new ProcessArtSettings();
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
        while (true)
        {
            if (ConfigCache.TryGetValue(path, out var existing))
                yield return existing;
            if (File.Exists(Path.Combine(Folder, path, "images.yaml")))
            {
                var config = new ArtConfig(this, Folder, path);
                ConfigCache[path] = config;
                yield return config;
            }

            if (path == "")
                break;
            path = Path.GetDirectoryName(path);
        }
    }

    private string? GetTemplatePath(string path)
    {
        var name = Path.Combine(Folder, path);
        if (!Directory.Exists(Path.GetDirectoryName(name)))
            return null;
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(name)))
        {
            if (Path.GetFileNameWithoutExtension(file) == Path.GetFileName(name))
                return file;
        }

        return null;
    }
}