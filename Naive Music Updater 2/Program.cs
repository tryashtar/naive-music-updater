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
            Console.OutputEncoding = Encoding.UTF8;

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
#if !DEBUG
            try
#endif
            {
                library.UpdateLibrary();
                library.UpdateSources();
                library.CheckSelectors();
            }
#if !DEBUG
            catch (Exception ex)
            {
                Logger.WriteLine(ex.ToString());
                Console.ReadLine();
            }
#endif
            Logger.Close();
#if DEBUG
            Console.ReadLine();
#endif
        }
    }
}
