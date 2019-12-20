using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class Album : IHaveArt
    {
        public List<SubAlbum> SubAlbums;
        // these are only songs directly inside this album, not those inside this album's subalbums
        public List<Song> Songs;
        public string Folder;
        public string FolderName;
        public string Name;
        public Artist Parent;
        public TagLib.Picture Art;
        public Album(string folder, Artist parent)
        {
            Parent = parent;
            Folder = folder;
            FolderName = Path.GetFileName(Folder);
            Name = NameRetriever.GetName(FolderName);
            Art = ArtRetriever.GetArt(GetArtLocation());
            SubAlbums = new List<SubAlbum>();
            foreach (var album in Directory.EnumerateDirectories(folder))
            {
                SubAlbums.Add(new SubAlbum(album, this));
            }
            Songs = new List<Song>();
            foreach (var song in Directory.EnumerateFiles(folder, "*.mp3"))
            {
                Songs.Add(new Song(song, this));
            }
        }

        public string GetArtLocation()
        {
            return Path.Combine(Parent.FolderName, FolderName);
        }

        public void Save()
        {
            Logger.WriteLine($"ALBUM: {Name}");
            Logger.WriteLine($"ART: {GetArtLocation()}");

            string albumini = Path.Combine(Folder, "desktop.ini");
            File.Delete(albumini);
            File.WriteAllText(albumini, "[.ShellClassInfo]\nIconResource = ..\\..\\.music-cache\\art\\" + GetArtLocation() + ".ico, 0");
            File.SetAttributes(albumini, FileAttributes.System | FileAttributes.Hidden);

            foreach (var subalbum in SubAlbums)
            {
                subalbum.Save();
            }
            foreach (var song in Songs)
            {
                song.Save();
            }
        }

        public IEnumerable<Song> AllSongs()
        {
            return Songs.Concat(SubAlbums.SelectMany(x => x.Songs));
        }
    }
}
