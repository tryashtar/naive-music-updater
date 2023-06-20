using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace NaiveMusicUpdater;

// these "Has" fields are for merging with other settings
// thus we can differentiate between unspecified, and explicitly disabled
public class ProcessArtSettings
{
    private bool SetWidth = false;
    public int? Width;
    private bool SetHeight = false;
    public int? Height;
    private bool HasBuffer = false;
    public int[]? Buffer;
    private bool HasBackground = false;
    public Rgba32? Background;
    public ResizeMode Scale = ResizeMode.Pad;
    public IResampler Interpolation = KnownResamplers.Bicubic;
    public bool IntegerScale = false;

    public ProcessArtSettings()
    {
    }

    public ProcessArtSettings(YamlMappingNode node)
    {
        if (node.Children.TryGetValue("width", out var w))
        {
            SetWidth = true;
            Width = w.String() == "original" ? null : w.Int();
        }

        if (node.Children.TryGetValue("height", out var h))
        {
            SetHeight = true;
            Height = h.String() == "original" ? null : h.Int();
        }

        if (node.Children.TryGetValue("buffer", out var b))
        {
            HasBuffer = true;
            var bb = b.Bool();
            Buffer = bb == false ? null : b.ToList(x => x.Int() ?? 0)!.ToArray();
        }

        if (node.Children.TryGetValue("background", out var bg))
        {
            HasBackground = true;
            var bb = bg.Bool();
            if (bb == false)
                Background = null;
            else
            {
                var list = bg.ToList(x => (byte)(x.Int() ?? 0))!;
                Background = Color.FromRgba(list[0], list[1], list[2], list[3]);
            }
        }

        if (node.Children.TryGetValue("scale", out var s))
            Scale = s.ToEnum<ResizeMode>()!.Value;
        if (node.Children.TryGetValue("interpolation", out var i))
        {
            if (i.String() == "bicubic")
                Interpolation = KnownResamplers.Bicubic;
            else if (i.String() == "nearest_neighbor")
                Interpolation = KnownResamplers.NearestNeighbor;
        }

        if (node.Children.TryGetValue("integer_scale", out var iscale))
            IntegerScale = iscale.Bool()!.Value;
    }

    public void MergeWith(ProcessArtSettings other)
    {
        if (other.SetWidth)
        {
            this.Width = other.Width;
            this.SetWidth = true;
        }

        if (other.SetHeight)
        {
            this.Height = other.Height;
            this.SetHeight = true;
        }

        if (other.HasBuffer)
        {
            this.Buffer = other.Buffer;
            this.HasBuffer = true;
        }

        if (other.HasBackground)
        {
            this.Background = other.Background;
            this.HasBackground = true;
        }

        this.Scale = other.Scale;
        this.Interpolation = other.Interpolation;
        this.IntegerScale = other.IntegerScale;
    }

    public void Apply(Image<Rgba32> image)
    {
        if (Buffer != null)
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
                if (IntegerScale)
                {
                    if (width > 0)
                        width = width / image.Width * image.Width;
                    if (height > 0)
                        height = height / image.Height * image.Height;
                }

                if (Buffer != null)
                {
                    width -= Buffer[0] + Buffer[2];
                    height -= Buffer[1] + Buffer[3];
                }

                var resize = new ResizeOptions()
                {
                    Mode = Scale,
                    Sampler = Interpolation,
                    Size = new(width, height)
                };
                if (Background != null)
                    resize.PadColor = Background.Value;
                x.Resize(resize);
            }

            if (Buffer != null)
            {
                var resize = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Size = new(x.GetCurrentSize().Width + Buffer[2],
                        x.GetCurrentSize().Height + Buffer[3]),
                    Position = AnchorPositionMode.TopLeft
                };
                x.Resize(resize);

                var resize2 = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Size = new(x.GetCurrentSize().Width + Buffer[0],
                        x.GetCurrentSize().Height + Buffer[1]),
                    Position = AnchorPositionMode.BottomRight
                };
                x.Resize(resize2);
            }

            if (Background != null)
                x.BackgroundColor(Background.Value);
        });

        if (Background != null)
        {
            var bg = Background.Value;
            if (bg.A == 0 && (bg.R != 0 || bg.G != 0 || bg.B != 0))
            {
                image.ProcessPixelRows(access =>
                {
                    for (int y = 0; y < access.Height; y++)
                    {
                        var row = access.GetRowSpan(y);
                        for (int x = 0; x < row.Length; x++)
                        {
                            ref var pixel = ref row[x];
                            if (pixel.A == 0)
                                pixel = bg;
                        }
                    }
                });
            }
        }
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
                var row = access.GetRowSpan(y);
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