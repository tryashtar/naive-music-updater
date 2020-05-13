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
    }

    public class MusicFolder : IMusicItem
    {
        public string Location { get; private set; }
        protected readonly MusicFolder _Parent;
        public MusicFolder Parent => _Parent;
        protected List<MusicFolder> Children;
        protected List<Song> SongList;
        public IReadOnlyList<MusicFolder> SubFolders { get { return Children.AsReadOnly(); } }
        public IReadOnlyList<Song> Songs { get { return SongList.AsReadOnly(); } }
        public MusicFolder(string folder) : this(null, folder)
        { }

        private MusicFolder(MusicFolder parent, string folder)
        {
            Location = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).TrimEnd('.');
            _Parent = parent;
            ScanContents();
        }

        public IEnumerable<Song> GetAllSongs()
        {
            return SongList.Concat(Children.SelectMany(x => x.GetAllSongs()));
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

        public void Update(LibraryCache cache)
        {
            Logger.WriteLine($"Folder: {SimpleName}");
            var filename = cache.ToFilesafe(cache.CleanName(SimpleName), true);
            if (SimpleName != filename)
            {
                Logger.WriteLine($"Renaming folder: \"{filename}\"");
                var newpath = Path.Combine(Path.GetDirectoryName(Location), filename);
                Util.MoveDirectory(Location, newpath);
                Location = newpath;
                ScanContents();
            }

            var art = cache.GetArtPathFor(this);
            ArtCache.LoadAndMakeIcon(art);
            string subalbumini = Path.Combine(Location, "desktop.ini");
            File.Delete(subalbumini);
            if (art != null)
            {
                File.WriteAllText(subalbumini, $"[.ShellClassInfo]\nIconResource = {Path.ChangeExtension(Util.RelativePath(Location, art), ".ico")}, 0");
                File.SetAttributes(subalbumini, FileAttributes.System | FileAttributes.Hidden);
            }

            Logger.TabIn();
            foreach (var child in Children)
            {
                child.Update(cache);
            }
            foreach (var song in SongList)
            {
                song.Update(cache);
            }
            Logger.TabOut();
        }

        private void ScanContents()
        {
            Children = new List<MusicFolder>();
            foreach (var dir in Directory.EnumerateDirectories(Location))
            {
                var child = new MusicFolder(this, dir);
                if (child.SongList.Any() || child.SubFolders.Any())
                    Children.Add(child);
            }
            SongList = new List<Song>();
            foreach (var file in Directory.EnumerateFiles(Location, "*.mp3"))
            {
                SongList.Add(new Song(this, file));
            }
        }
    }
}
