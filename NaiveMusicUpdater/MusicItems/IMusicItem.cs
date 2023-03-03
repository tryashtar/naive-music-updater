namespace NaiveMusicUpdater;

public interface IMusicItem
{
    string Location { get; }
    string SimpleName { get; }
    MusicFolder? Parent { get; }
    IMusicItemConfig[] Configs { get; }
    MusicLibrary RootLibrary { get; }
}
