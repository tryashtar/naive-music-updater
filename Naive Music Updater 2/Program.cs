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
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Logger.WriteLine("NAIVE MUSIC UPDATER");

            // allows album art to show up in explorer
            TagLib.Id3v2.Tag.DefaultVersion = 3;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;

#if DEBUG
            if (args.Length > 0)
            {
                try
                {
                    PrintFrames(args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                Console.ReadLine();
                return;
            }
#endif

            string FolderPath;
#if DEBUG
            FolderPath = File.ReadAllText("folder.txt");
#else
            FolderPath = Directory.GetCurrentDirectory();
#endif
#if !DEBUG
            try
#endif
            {
                var library = new MusicLibrary(FolderPath);
                library.UpdateLibrary();
                Logger.WriteLine();
                library.UpdateSources();
                Logger.WriteLine();
                var results = library.CheckSelectors();
# if !DEBUG
                if (results.AnyUnused)
                    Console.ReadLine();
#endif
            }
#if !DEBUG
            catch (Exception ex)
            {
                Logger.WriteLine(ex.ToString());
                Console.ReadLine();
            }
#endif
            Logger.Close();
        }

        public static void PrintFrames(string[] paths)
        {
            foreach (var item in paths)
            {
                if (File.Exists(item))
                {
                    var song = TagLib.File.Create(item);
                    Console.WriteLine(item);
                    var tag = song.Tag;
                    PrintTag(tag);
                }
            }
        }

        private static void PrintTag(TagLib.Tag tag)
        {
            var name = tag.GetType().ToString();
            Console.WriteLine(name);
            Console.WriteLine(new String('-', name.Length));
            if (tag is TagLib.CombinedTag combined)
            {
                foreach (var sub in combined.Tags)
                {
                    PrintTag(sub);
                }
            }
            if (tag is TagLib.Id3v2.Tag id3v2)
            {
                foreach (var frame in id3v2.GetFrames().ToList())
                {
                    Console.WriteLine(FrameString(frame));
                }
            }
            if (tag is TagLib.Ape.Tag ape)
            {
                foreach (var key in ape)
                {
                    var value = ape.GetItem(key);
                    Console.WriteLine($"{key}: {value}");
                }
            }
            if (tag is TagLib.Ogg.XiphComment xiph)
            {
                foreach (var key in xiph)
                {
                    var value = xiph.GetField(key);
                    Console.WriteLine($"{key}: {String.Join("\n", value)}");
                }
            }
            Console.WriteLine(new String('-', name.Length));
        }

        private static string FrameString(TagLib.Id3v2.Frame frame)
        {
            var builder = new StringBuilder();
            builder.AppendLine(frame.GetType().ToString());
            builder.AppendLine(frame.FrameId.ToString());
            builder.AppendLine(frame.ToString());
            return builder.ToString();
        }
    }
}
