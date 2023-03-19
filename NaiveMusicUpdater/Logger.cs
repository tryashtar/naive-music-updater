namespace NaiveMusicUpdater;

// static logger, because you're insane if you think I'm passing it into every constructor
// prints to the console, and also optionally to a file
public static class Logger
{
    private static StreamWriter? Writer;
    private static string UnwrittenData = "";
    private static int TabCount = 0;

    public static void Open(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Writer = new StreamWriter(File.Create(path));
        Writer.Write(UnwrittenData);
        UnwrittenData = "";
    }

    public static void Close()
    {
        Writer?.Close();
    }

    // if we have a file opened, write to it
    // otherwise, save this text to write to the file when we do open one
    private static void Write(string text)
    {
        if (Writer == null)
            UnwrittenData += text + Environment.NewLine;
        else
            Writer.WriteLine(text);
    }

    public static void WriteLine() => WriteLine("");

    public static void WriteLine(string? text, ConsoleColor color = ConsoleColor.White)
    {
        string tabs = new('\t', TabCount);
        if (text != null)
            text = text.Replace("\n", '\n' + tabs);
        var prev_color = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(tabs + text);
        Console.ForegroundColor = prev_color;
        Write(tabs + text);
    }

    public static string ReadLine()
    {
        string text = Console.ReadLine() ?? "";
        string tabs = new string('\t', TabCount);
        Write(tabs + text);
        return text;
    }

    public static void TabIn() => TabCount++;
    public static void TabOut() => TabCount--;
}