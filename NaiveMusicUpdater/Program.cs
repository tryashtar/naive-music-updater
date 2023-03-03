namespace NaiveMusicUpdater;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Logger.WriteLine("NAIVE MUSIC UPDATER");

        // allows album art to show up in explorer
        TagLib.Id3v2.Tag.DefaultVersion = 3;
        TagLib.Id3v2.Tag.ForceDefaultVersion = true;

        if (args.Length > 0 && args[0] == "--print-tags")
        {
            PrintTags(args[1..]);
            return;
        }

        string library_yaml = args.Length > 0 ? args[0] : "library.yaml";
        if (!File.Exists(library_yaml))
        {
            Logger.WriteLine($"File {library_yaml} not found.", ConsoleColor.Red);
            if (args.Length == 0)
                Logger.WriteLine($"Specify the path to one as the first argument.", ConsoleColor.Red);
        }
        else
            WrapException(() => CreateAndUpdateLibrary(library_yaml));

        Logger.Close();
    }

    private static void CreateAndUpdateLibrary(string path)
    {
        var config = new LibraryConfig(path);
        var library = new MusicLibrary(config);
        library.UpdateLibrary();
        Logger.WriteLine();
        var results = library.CheckSelectors();
        if (results.AnyUnused)
            Console.ReadLine();
    }

    private static void WrapException(Action action)
    {
#if DEBUG
        action();
#else
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Logger.WriteLine(ex.ToString(), ConsoleColor.Red);
            Console.ReadLine();
        }
#endif
    }

    public static void PrintTags(IEnumerable<string> paths)
    {
        foreach (var item in paths)
        {
            if (File.Exists(item))
            {
                var song = TagLib.File.Create(item);
                Logger.WriteLine(item, ConsoleColor.Green);
                var tag = song.Tag;
                PrintTag(tag);
            }
            else
                Logger.WriteLine($"Not found: {item}", ConsoleColor.Red);
            Logger.WriteLine();
        }
    }

    private static void PrintTag(TagLib.Tag tag)
    {
        var name = tag.GetType().ToString();
        Logger.WriteLine(name, ConsoleColor.Yellow);

        if (tag is TagLib.Id3v2.Tag id3v2)
        {
            foreach (var frame in id3v2.GetFrames().ToList())
            {
                Logger.WriteLine(FrameViewer.ToString(frame));
            }
        }
        
        if (tag is TagLib.Id3v1.Tag id3v1)
        {
            Logger.WriteLine(id3v1.Render().ToString());
        }

        if (tag is TagLib.Flac.Metadata flac)
        {
            foreach (var pic in flac.Pictures)
            {
                Logger.WriteLine(pic.ToString());
            }
        }

        if (tag is TagLib.Ape.Tag ape)
        {
            foreach (var key in ape)
            {
                var value = ape.GetItem(key);
                Logger.WriteLine($"{key}: {value}");
            }
        }

        if (tag is TagLib.Ogg.XiphComment xiph)
        {
            foreach (var key in xiph)
            {
                var value = xiph.GetField(key);
                Logger.WriteLine($"{key}: {String.Join("\n", value)}");
            }
        }
        
        if (tag is TagLib.CombinedTag combined)
        {
            foreach (var sub in combined.Tags)
            {
                Logger.TabIn();
                PrintTag(sub);
                Logger.TabOut();
            }
        }
    }
}