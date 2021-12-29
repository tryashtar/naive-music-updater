namespace NaiveMusicUpdater;

public interface IMusicItem
{
    IEnumerable<IMusicItem> PathFromRoot();
    string Location { get; }
    string SimpleName { get; }
    MusicFolder? Parent { get; }
    MusicItemConfig? LocalConfig { get; }
    LibraryCache GlobalCache { get; }
    MusicLibrary RootLibrary { get; }
}
