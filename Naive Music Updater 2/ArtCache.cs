using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;

namespace NaiveMusicUpdater
{
    public static class ArtCache
    {
        public static Dictionary<string, TagLib.IPicture> Cached = new Dictionary<string, TagLib.IPicture>();
        public static TagLib.IPicture GetPicture(string path)
        {
            if (Cached.TryGetValue(path, out var result))
                return result;
            var art = LoadAndMakeIcon(path);
            if (art == null)
                return null;
            Cached[path] = art;
            return art;
        }

        public static TagLib.IPicture LoadAndMakeIcon(string png)
        {
            if (!File.Exists(png))
                return null;
            var image = Image.FromFile(png);
            string ico = Path.ChangeExtension(png, ".ico");
            using (image)
            {
                byte[] bytes = ConvertToIcon(image, true);
                if (!File.Exists(ico) || !File.ReadAllBytes(ico).SequenceEqual(bytes))
                    File.WriteAllBytes(ico, ConvertToIcon(image, true));
                var icon = new TagLib.Picture(new TagLib.ByteVector((byte[])new ImageConverter().ConvertTo(image, typeof(byte[]))));
                return icon;
            }
        }

        private static byte[] ConvertToIcon(Image image, bool preserveAspectRatio = false)
        {
            MemoryStream inputStream = new MemoryStream();
            image.Save(inputStream, ImageFormat.Png);
            inputStream.Seek(0, SeekOrigin.Begin);
            MemoryStream outputStream = new MemoryStream();
            if (!ConvertToIcon(inputStream, outputStream, 256, preserveAspectRatio))
                return null;
            return outputStream.ToArray();
        }

        private static bool ConvertToIcon(Stream input, Stream output, int size = 256, bool preserveAspectRatio = false)
        {
            var inputBitmap = (Bitmap)Bitmap.FromStream(input);
            if (inputBitmap == null)
                return false;
            float width = size, height = size;
            if (preserveAspectRatio)
            {
                if (inputBitmap.Width > inputBitmap.Height)
                    height = ((float)inputBitmap.Height / inputBitmap.Width) * size;
                else
                    width = ((float)inputBitmap.Width / inputBitmap.Height) * size;
            }
            var newBitmap = new Bitmap(inputBitmap, new Size((int)width, (int)height));
            if (newBitmap == null)
                return false;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                newBitmap.Save(memoryStream, ImageFormat.Png);
                var iconWriter = new BinaryWriter(output);
                if (output == null || iconWriter == null)
                    return false;
                iconWriter.Write((byte)0);
                iconWriter.Write((byte)0);
                iconWriter.Write((short)1);
                iconWriter.Write((short)1);
                iconWriter.Write((byte)width);
                iconWriter.Write((byte)height);
                iconWriter.Write((byte)0);
                iconWriter.Write((byte)0);
                iconWriter.Write((short)0);
                iconWriter.Write((short)32);
                iconWriter.Write((int)memoryStream.Length);
                iconWriter.Write((int)(6 + 16));
                iconWriter.Write(memoryStream.ToArray());
                iconWriter.Flush();
            }
            return true;
        }
    }
}
