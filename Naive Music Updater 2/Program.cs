using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public static class Program
    {
        public static void Main()
        {
            // allows album art to show up in explorer
            TagLib.Id3v2.Tag.DefaultVersion = 3;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;

            string FolderPath;
#if DEBUG
            FolderPath = @"D:\Music";
#else
            FolderPath = Directory.GetCurrentDirectory();
#endif
            var library = new MusicLibrary(FolderPath);
            library.Update();
#if DEBUG
            Console.ReadLine();
#endif
        }
    }
}
