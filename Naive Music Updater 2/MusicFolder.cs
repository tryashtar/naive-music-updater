using System;
using System.Collections.Generic;
using System.IO;
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
        SongMetadata GetMetadata();
    }

    public static class MusicItemImplementations
    {
        public static SongMetadata GetMetadata(this IMusicItem item)
        {
            var path = item.PathFromRoot();
            var metas = new List<SongMetadata>();
            metas.Add(item.GlobalCache.Config.GetMetadataFor(item));
            foreach (var entry in path)
            {
                if (entry.LocalConfig != null)
                    metas.Add(entry.LocalConfig.GetMetadataFor(item));
            }
            return SongMetadata.Merge(metas);
        }
    }

    public class MusicFolder : IMusicItem
    {
        public string Location { get; private set; }
        protected readonly MusicItemConfig _LocalConfig;
        public MusicItemConfig LocalConfig => _LocalConfig;
        public virtual LibraryCache GlobalCache => _Parent.GlobalCache;
        protected readonly MusicFolder _Parent;
        public MusicFolder Parent => _Parent;
        protected List<MusicFolder> ChildFolders;
        protected List<Song> SongList;
        public IReadOnlyList<MusicFolder> SubFolders { get { return ChildFolders.AsReadOnly(); } }
        public IReadOnlyList<Song> Songs { get { return SongList.AsReadOnly(); } }
        public MusicFolder(string folder) : this(null, folder)
        { }

        private MusicFolder(MusicFolder parent, string folder)
        {
            Location = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).TrimEnd('.');
            _Parent = parent;
            string config = Path.Combine(folder, "config.yaml");
            if (File.Exists(config))
                _LocalConfig = new MusicItemConfig(this, config);
            ScanContents();
        }

        public SongMetadata GetMetadata() => MusicItemImplementations.GetMetadata(this);

        public IEnumerable<Song> GetAllSongs()
        {
            return SongList.Concat(ChildFolders.SelectMany(x => x.GetAllSongs()));
        }

        public string SimpleName => Path.GetFileName(Location);

        public IEnumerable<IMusicItem> PathFromRoot()
        {
            var list = new List<IMusicItem>();
            if (this._Parent != null)
                list.AddRange(this._Parent.PathFromRoot());
            list.Add(this);
            return list;
        }

        public void Update()
        {
            Logger.WriteLine($"Folder: {SimpleName}");
            var filename = GlobalCache.Config.ToFilesafe(GlobalCache.Config.CleanName(SimpleName), true);
            if (SimpleName != filename)
            {
                Logger.WriteLine($"Renaming folder: \"{filename}\"");
                var newpath = Path.Combine(Path.GetDirectoryName(Location), filename);
                Util.MoveDirectory(Location, newpath);
                Location = newpath;
                ScanContents();
            }

            var art = GlobalCache.GetArtPathFor(this);
            ArtCache.LoadAndMakeIcon(art);
            string subalbumini = Path.Combine(Location, "desktop.ini");
            File.Delete(subalbumini);
            if (art != null)
            {
                File.WriteAllText(subalbumini, $"[.ShellClassInfo]\nIconResource = {Path.ChangeExtension(Util.RelativePath(Location, art), ".ico")}, 0");
                File.SetAttributes(subalbumini, FileAttributes.System | FileAttributes.Hidden);
            }

            Logger.TabIn();
            foreach (var child in ChildFolders)
            {
                child.Update();
            }
            foreach (var song in SongList)
            {
                song.Update();
            }
            Logger.TabOut();
        }

        private void ScanContents()
        {
            ChildFolders = new List<MusicFolder>();
            var info = new DirectoryInfo(Location);
            foreach (DirectoryInfo dir in info.EnumerateDirectories())
            {
                if (dir.Attributes.HasFlag(FileAttributes.Hidden))
                    continue;
                var child = new MusicFolder(this, dir.FullName);
                if (child.SongList.Any() || child.SubFolders.Any())
                    ChildFolders.Add(child);
            }
            SongList = new List<Song>();
            foreach (var file in Directory.EnumerateFiles(Location, "*.mp3"))
            {
                SongList.Add(new Song(this, file));
            }
        }
    }
}
