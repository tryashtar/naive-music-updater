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
}