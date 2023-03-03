namespace NaiveMusicUpdater;

public interface IMusicItem
{
    IEnumerable<IMusicItem> PathFromRoot();
    string Location { get; }
    string SimpleName { get; }
    MusicFolder? Parent { get; }
    IMusicItemConfig[] Configs { get; }
    LibraryConfig GlobalConfig { get; }
    MusicLibrary RootLibrary { get; }
    sealed string StringPathAfterRoot()
    {
        return String.Join(Path.DirectorySeparatorChar.ToString(), PathFromRoot().Skip(1).Select(x => x.SimpleName));
    }
}
