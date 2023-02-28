namespace NaiveMusicUpdater;

public static class Util
{
    public static string StringPathAfterRoot(IMusicItem item)
    {
        return String.Join(Path.DirectorySeparatorChar.ToString(), item.PathFromRoot().Skip(1).Select(x => x.SimpleName));
    }

    public static void MoveDirectory(string from, string to)
    {
        var temp_windows_hack = to + "_TEMPORARY_FOLDER";
        Directory.Move(from, temp_windows_hack);
        Directory.Move(temp_windows_hack, to);
    }
}
