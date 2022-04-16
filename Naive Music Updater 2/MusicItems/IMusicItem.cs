namespace NaiveMusicUpdater;

public interface IMusicItem
{
    IEnumerable<IMusicItem> PathFromRoot();
    string Location { get; }
    string SimpleName { get; }
    MusicFolder? Parent { get; }
    IMusicItemConfig? LocalConfig { get; }
    LibraryCache GlobalCache { get; }
    MusicLibrary RootLibrary { get; }
}
