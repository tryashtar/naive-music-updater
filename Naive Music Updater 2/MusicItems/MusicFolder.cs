using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
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
        public IEnumerable<IMusicItem> SubItems => ChildFolders.Concat<IMusicItem>(SongList);
        public MusicFolder(string folder) : this(null, folder)
        { }

        private MusicFolder(MusicFolder parent, string folder)
        {
            Location = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).TrimEnd('.');
            _Parent = parent;
            ScanContents();
            string config = Path.Combine(folder, "config.yaml");
            if (File.Exists(config))
                _LocalConfig = new MusicItemConfig(config, this);
        }

        public IEnumerable<Song> GetAllSongs()
        {
            return SongList.Concat(ChildFolders.SelectMany(x => x.GetAllSongs()));
        }

        public IEnumerable<IMusicItem> GetAllSubItems()
        {
            return SubItems.Concat(ChildFolders.SelectMany(x => x.GetAllSubItems()));
        }

        public string SimpleName => Path.GetFileName(Location);

        public IEnumerable<IMusicItem> PathFromRoot() => MusicItemUtils.PathFromRoot(this);
        public MusicLibrary RootLibrary => (MusicLibrary)PathFromRoot().First();

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

            var metadata = MusicItemUtils.GetMetadata(this, MetadataField.All);
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
