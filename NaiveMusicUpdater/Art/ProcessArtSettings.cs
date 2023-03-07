using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace NaiveMusicUpdater;

public class ProcessArtSettings
{
    public int? Width;
    public int? Height;
    public bool? HasBuffer;
    public int[]? Buffer;
    public Color? Background;
    public ResizeMode? Scale;
    public IResampler? Interpolation;
    public bool? IntegerScale;

    public ProcessArtSettings()
    {
    }

    public ProcessArtSettings(YamlMappingNode node)
    {
        if (node.Children.TryGetValue("width", out var w))
            Width = w.Int();
        if (node.Children.TryGetValue("height", out var h))
            Height = h.Int();
        if (node.Children.TryGetValue("buffer", out var b))
        {
            var bb = b.Bool();
            if (bb == false)
            {
                HasBuffer = false;
                Buffer = null;
            }
            else
            {
                HasBuffer = true;
                Buffer = b.ToList(x => x.Int().Value).ToArray();
            }
        }

        if (node.Children.TryGetValue("background", out var bg))
        {
            var list = bg.ToList(x => (byte)x.Int().Value);
            Background = Color.FromRgba(list[0], list[1], list[2], list[3]);
        }

        if (node.Children.TryGetValue("scale", out var s))
            Scale = s.ToEnum<ResizeMode>();
        if (node.Children.TryGetValue("interpolation", out var i))
        {
            if (i.String() == "bicubic")
                Interpolation = KnownResamplers.Bicubic;
            else if (i.String() == "nearest_neighbor")
                Interpolation = KnownResamplers.NearestNeighbor;
        }

        if (node.Children.TryGetValue("integer_scale", out var iscale))
            IntegerScale = iscale.Bool();
    }

    public void MergeWith(ProcessArtSettings other)
    {
        this.Width ??= other.Width;
        this.Height ??= other.Height;
        this.HasBuffer ??= other.HasBuffer;
        this.Buffer ??= other.Buffer;
        this.Background ??= other.Background;
        this.Scale ??= other.Scale;
        this.Interpolation ??= other.Interpolation;
        this.IntegerScale ??= other.IntegerScale;
    }

    public void Apply(Image<Rgba32> image)
    {
        if (HasBuffer ?? false)
        {
            var bounding = GetBoundingRectangle(image);
            image.Mutate(x => x.Crop(bounding));
        }

        image.Mutate(x =>
        {
            if (Width != null || Height != null)
            {
                int width = Width ?? 0;
                int height = Height ?? 0;
                if (IntegerScale ?? false)
                {
                    if (width > 0)
                        width = width / image.Width * image.Width;
                    if (height > 0)
                        height = height / image.Height * image.Height;
                }

                if (HasBuffer ?? false)
                {
                    width -= Buffer[0] + Buffer[2];
                    height -= Buffer[1] + Buffer[3];
                }

                var resize = new ResizeOptions()
                {
                    Mode = Scale ?? ResizeMode.BoxPad,
                    Sampler = Interpolation ?? KnownResamplers.Bicubic,
                    Size = new(width, height)
                };
                if (Background != null)
                    resize.PadColor = Background.Value;
                x.Resize(resize);
            }

            if (HasBuffer ?? false)
            {
                var resize = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Size = new(x.GetCurrentSize().Width + Buffer[2],
                        x.GetCurrentSize().Height + Buffer[3]),
                    Position = AnchorPositionMode.TopLeft
                };
                if (Background != null)
                    resize.PadColor = Background.Value;
                x.Resize(resize);

                var resize2 = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Size = new(x.GetCurrentSize().Width + Buffer[0],
                        x.GetCurrentSize().Height + Buffer[1]),
                    Position = AnchorPositionMode.BottomRight
                };
                if (Background != null)
                    resize2.PadColor = Background.Value;
                x.Resize(resize2);
            }

            if (Background != null)
                x.BackgroundColor(Background.Value);
        });
    }

    private static Rectangle GetBoundingRectangle(Image<Rgba32> image)
    {
        int left = image.Width;
        int top = image.Height;
        int right = 0;
        int bottom = 0;
        image.ProcessPixelRows(access =>
        {
            for (int y = 0; y < access.Height; y++)
            {
                Span<Rgba32> row = access.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ref var pixel = ref row[x];
                    if (pixel.A != 0)
                    {
                        left = Math.Min(x, left);
                        top = Math.Min(y, top);
                        right = Math.Max(x, right);
                        bottom = Math.Max(y, bottom);
                    }
                }
            }
        });
        return new(left, top, right - left + 1, bottom - top + 1);
    }
}