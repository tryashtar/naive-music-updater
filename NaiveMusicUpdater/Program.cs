namespace NaiveMusicUpdater;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Logger.WriteLine("NAIVE MUSIC UPDATER");

        // allows album art to show up in file explorer on Windows
        TagLib.Id3v2.Tag.DefaultVersion = 3;
        TagLib.Id3v2.Tag.ForceDefaultVersion = true;

        if (args.Length > 0 && args[0] == "--print-tags")
        {
            TagPrinter.PrintTags(args[1..]);
            return;
        }

        string library_yaml = args.Length > 0 ? args[0] : "library.yaml";
        if (!File.Exists(library_yaml))
        {
            Logger.WriteLine($"File {library_yaml} not found.", ConsoleColor.Red);
            if (args.Length == 0)
                Logger.WriteLine("Specify the path to one as the first argument.", ConsoleColor.Red);
        }
        else
            WrapException(() => CreateAndUpdateLibrary(library_yaml));

        Logger.Close();
    }

    private static void CreateAndUpdateLibrary(string path)
    {
        var config = new LibraryConfig(path);
        var library = new MusicLibrary(config);
        if (config.LogFolder != null)
            Logger.Open(Path.Combine(config.LogFolder, DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss") + ".txt"));
        library.UpdateLibrary();
    }

    // while debugging, we want to break on exceptions
    // in release mode, we want to print them and pause for a chance to read
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
}