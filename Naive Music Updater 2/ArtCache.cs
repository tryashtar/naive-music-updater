using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace NaiveMusicUpdater;

public static class ArtCache
{
    public static Dictionary<string, IPicture> Cached = new();
    public static IPicture? GetPicture(string path)
    {
        if (Cached.TryGetValue(path, out var result))
            return result;
        var art = LoadAndMakeIcon(path);
        if (art == null)
            return null;
        Cached[path] = art;
        return art;
    }

    public static IPicture? LoadAndMakeIcon(string png)
    {
        if (!File.Exists(png))
            return null;
        var image = Image.FromFile(png);
        string ico = Path.ChangeExtension(png, ".ico");
        using (image)
        {
            using (var stream = ConvertToIcon(image))
            {
                var bytes = stream.ToArray();
                if (!File.Exists(ico) || !File.ReadAllBytes(ico).SequenceEqual(bytes))
                {
                    Logger.WriteLine($"Creating icon");
                    File.WriteAllBytes(ico, bytes);
                }
            }
            var icon = new Picture(new ByteVector((byte[])new ImageConverter().ConvertTo(image, typeof(byte[]))));
            return icon;
        }
    }

    // convert to a 256x256 icon, preserving aspect ratio
    private static MemoryStream ConvertToIcon(Image image)
    {
        int width = 256;
        int height = 256;
        float ratio = (float)image.Width / image.Height;
        if (image.Width > image.Height)
            height = (int)(ratio * 256);
        else
            width = (int)(ratio * 256);
        var square_image = new Bitmap(256, 256);
        using (var graphics = Graphics.FromImage(square_image))
        {
            int y = (256 / 2) - (height / 2);
            int x = (256 / 2) - (width / 2);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(image, x, y, width, height);
        }
        using (var source_stream = new MemoryStream())
        {
            square_image.Save(source_stream, ImageFormat.Png);
            var output_stream = new MemoryStream();
            var writer = new BinaryWriter(output_stream);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write((byte)square_image.Width);
            writer.Write((byte)square_image.Height);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)0);
            writer.Write((short)32);
            writer.Write((int)source_stream.Length);
            writer.Write((int)(6 + 16));
            source_stream.WriteTo(output_stream);
            writer.Flush();
            output_stream.Position = 0;
            return output_stream;
        }
    }
}
