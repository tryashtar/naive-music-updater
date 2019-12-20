using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public static class ArtRetriever
    {
        private static string SourceFolder;
        private static Dictionary<string, TagLib.Picture> Gallery;
        static ArtRetriever()
        {
            Gallery = new Dictionary<string, TagLib.Picture>();
        }

        public static void SetArtSource(string folder, SearchOption search)
        {
            SourceFolder = folder;
            Gallery.Clear();
            foreach (var png in Directory.GetFiles(folder, "*.png", search))
            {
                string ico = Path.ChangeExtension(png, ".ico");
                var image = Image.FromFile(png);
                using (image)
                {
                    byte[] bytes = ConvertToIcon(image, true);
                    if (!File.Exists(ico) || !File.ReadAllBytes(ico).SequenceEqual(bytes))
                        File.WriteAllBytes(ico, ConvertToIcon(image, true));
                    Gallery.Add(Path.ChangeExtension(png.Substring(folder.Length + 1), null), new TagLib.Picture(new TagLib.ByteVector((byte[])new ImageConverter().ConvertTo(image, typeof(byte[])))));
                }
            }
        }

        public static string FullLocation(string artname)
        {
            return Path.Combine(SourceFolder, artname + ".png");
        }

        // run after scanning to declare that the current state of art has been updated in the songs
        public static void MarkAllArtRead()
        {
            foreach (var artname in Gallery.Keys)
            {
                ModifiedOptimizer.RecordChange(FullLocation(artname));
            }
        }

        public static TagLib.Picture GetArt(string name)
        {
            if (Gallery.TryGetValue(name, out TagLib.Picture result))
                return result;
            return null;
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
