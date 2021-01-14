using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public interface IMusicItem
    {
        IEnumerable<IMusicItem> PathFromRoot();
        string Location { get; }
        string SimpleName { get; }
        MusicFolder Parent { get; }
        MusicItemConfig LocalConfig { get; }
        LibraryCache GlobalCache { get; }
        MusicLibrary RootLibrary { get; }
    }
}
