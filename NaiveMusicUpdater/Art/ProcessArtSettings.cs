using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NaiveMusicUpdater;

public class ProcessArtSettings
{
    public int? Width;
    public int? Height;
    public int? Buffer;
    public Color? Background;
    public ScaleOption? Scale;
    public InterpolationOption? Interpolation;
    public bool? IntegerScale;
    public bool? CropToContent;

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
            Buffer = b.Int();
        if (node.Children.TryGetValue("background", out var bg))
        {
            var list = bg.ToList(x => (byte)x.Int().Value);
            Background = Color.FromRgba(list[0], list[1], list[2], list[3]);
        }

        if (node.Children.TryGetValue("scale", out var s))
            Scale = s.ToEnum<ScaleOption>();
        if (node.Children.TryGetValue("interpolation", out var i))
            Interpolation = i.ToEnum<InterpolationOption>();
        if (node.Children.TryGetValue("integer_scale", out var iscale))
            IntegerScale = iscale.Bool();
        if (node.Children.TryGetValue("crop_to_content", out var cc))
            CropToContent = cc.Bool();
    }

    public void MergeWith(ProcessArtSettings other)
    {
        this.Width ??= other.Width;
        this.Height ??= other.Height;
        this.Buffer ??= other.Buffer;
        this.Background ??= other.Background;
        this.Scale ??= other.Scale;
        this.Interpolation ??= other.Interpolation;
        this.IntegerScale ??= other.IntegerScale;
        this.CropToContent ??= other.CropToContent;
    }
}

public enum ScaleOption
{
    MaintainRatioFill,
    MaintainRatioCrop
}

public enum InterpolationOption
{
    Bicubic,
    NearestNeighbor
}