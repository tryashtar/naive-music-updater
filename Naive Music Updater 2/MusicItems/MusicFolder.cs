using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class MusicFolder : IMusicItem
    {
        private bool HasScanned = false;
        public string Location { get; private set; }
        protected readonly MusicItemConfig _LocalConfig;
        public MusicItemConfig LocalConfig => _LocalConfig;
        public virtual LibraryCache GlobalCache => _Parent.GlobalCache;
        protected readonly MusicFolder _Parent;
        public MusicFolder Parent => _Parent;
        protected List<MusicFolder> ChildFolders;
        protected List<Song> SongList;
        public IReadOnlyList<MusicFolder> SubFolders
        {
            get
            {
                if (!HasScanned)
                    ScanContents();
                return ChildFolders.AsReadOnly();
            }
        }
        public IReadOnlyList<Song> Songs
        {
            get
            {
                if (!HasScanned)
                    ScanContents();
                return SongList.AsReadOnly();
            }
        }
        public IEnumerable<IMusicItem> SubItems => SubFolders.Concat<IMusicItem>(Songs);
        public MusicFolder(string folder) : this(null, folder)
        { }

        private MusicFolder(MusicFolder parent, string folder)
        {
            Location = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).TrimEnd('.');
            _Parent = parent;
            string config = Path.Combine(folder, "config.yaml");
            if (File.Exists(config))
            {
#if !DEBUG2
                _LocalConfig = new MusicItemConfig(config, this);
#else
                try
                {
                    _LocalConfig = new MusicItemConfig(config, this);
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"Failed to parse config for {this}: {ex.Message}");
                }
#endif
            }
        }

        public IEnumerable<Song> GetAllSongs()
        {
            return Songs.Concat(SubFolders.SelectMany(x => x.GetAllSongs()));
        }

        public IEnumerable<IMusicItem> GetAllSubItems()
        {
            return SubItems.Concat(SubFolders.SelectMany(x => x.GetAllSubItems()));
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

            //var metadata = MusicItemUtils.GetMetadata(this, MetadataField.All);
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
            foreach (var child in SubFolders)
            {
                child.Update();
            }
            foreach (var song in Songs)
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
                if (child.Songs.Any() || child.SubFolders.Any())
                    ChildFolders.Add(child);
            }
            SongList = new List<Song>();
            foreach (var file in Directory.EnumerateFiles(Location))
            {
                if (GlobalCache.Config.IsSongFile(file))
                    SongList.Add(new Song(this, file));
            }
            HasScanned = true;
        }

        public CheckSelectorResults CheckSelectors()
        {
            var answer = new CheckSelectorResults();
            if (LocalConfig != null)
            {
                var results = LocalConfig.CheckSelectors();
                if (results.UnusedSelectors.Any())
                {
                    Logger.WriteLine($"{this} has unused selectors:");
                    Logger.TabIn();
                    foreach (var unused in results.UnusedSelectors)
                    {
                        Logger.WriteLine(unused.ToString());
                    }
                    Logger.TabOut();
                }
                if (results.UnselectedItems.Any())
                {
                    Logger.WriteLine($"{this} has unselected items:");
                    Logger.TabIn();
                    foreach (var unselected in results.UnselectedItems)
                    {
                        Logger.WriteLine(unselected.SimpleName);
                    }
                    Logger.TabOut();
                }
                answer.AddResults(results);
            }
            foreach (var item in SubFolders)
            {
                answer.AddResults(item.CheckSelectors());
            }
            return answer;
        }

        public override string ToString()
        {
            return String.Join("/", MusicItemUtils.PathFromRoot(this).Select(x => x.SimpleName));
        }
    }
}
