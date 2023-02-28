﻿namespace NaiveMusicUpdater;

public interface IMusicItem
{
    IEnumerable<IMusicItem> PathFromRoot();
    string Location { get; }
    string SimpleName { get; }
    MusicFolder? Parent { get; }
    IMusicItemConfig? LocalConfig { get; }
    LibraryConfig GlobalConfig { get; }
    MusicLibrary RootLibrary { get; }
}
