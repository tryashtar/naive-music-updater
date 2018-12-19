using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using Naive_Music_Updater;

namespace CSharpFiddle
{
    public static class Program
    {
        private static string FolderPath;

        [STAThread]
        private static void Main()
        {
#if DEBUG
            FolderPath = @"D:\Music";
            NaiveSongUpdate(FolderPath);
            Console.ReadLine();
#else
            FolderPath = Directory.GetCurrentDirectory();
            NaiveSongUpdate(FolderPath);
#endif
        }

        // maybe make this a class or something, with settings for stuff like
        // - location of music cache
        // - "realnames" dictionary
        // - rename exceptions location
        // - whether to wipe certain properties
        // then main can load those from a file instead and pass them in
        // then just do NaiveUpdater.Start(string folder)

        // current need: subalbums
        // the "grouping" tag is now for subalbums, for example
        // Artist: Nintendo, Album: Smash Bros Ultimate, Subalbum/grouping: Yoshi, Title: Yoshi Story
        // Subalbum folders in the album folder, but songs in root also allowed
        // Subalbums can have art, and their songs use that art

        // embed three pictures -- subalbum, album, artist
        private static void NaiveSongUpdate(string folder)
        {
            TagLib.Id3v2.Tag.DefaultVersion = 3;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;

            // create art folder if it's not already there
            string cache = Path.Combine(folder, ".music-cache");
            DirectoryInfo di = Directory.CreateDirectory(cache);
            di.Attributes |= FileAttributes.System | FileAttributes.Hidden;

            string realnamesfile = Path.Combine(cache, "realnames.json");
            var realnames = new Dictionary<string, string>();
            if (File.Exists(realnamesfile))
                realnames = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(realnamesfile));

            NameRetriever.SetMap(realnames);
            NameRetriever.SetIgnoreList(File.ReadAllLines(Path.Combine(cache, "exceptions.txt")).ToList());
            ArtRetriever.SetArtSource(cache);
            Library library = new Library(folder);
            library.Save();
            Writer.Close(Path.Combine(cache, "logs"));
        }
    }
}
