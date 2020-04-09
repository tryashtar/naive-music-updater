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
    }

    public class MusicFolder : IMusicItem
    {
        public string Location { get; private set; }
        protected readonly MusicFolder Parent;
        protected List<MusicFolder> Children;
        protected List<Song> SongList;
        public IReadOnlyList<MusicFolder> SubFolders { get { return Children.AsReadOnly(); } }
        public IReadOnlyList<Song> Songs { get { return SongList.AsReadOnly(); } }
        public MusicFolder(string folder)
        {
            Location = folder;
            Parent = null;
            ScanContents();
        }

        private MusicFolder(MusicFolder parent, string folder)
        {
            Location = folder;
            Parent = parent;
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
            if (this.Parent != null)
                list.AddRange(this.Parent.PathFromRoot());
            list.Add(this);
            return list;
        }

        public void Update(LibraryCache cache)
        {
            Logger.WriteLine($"Folder: {SimpleName}");
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
