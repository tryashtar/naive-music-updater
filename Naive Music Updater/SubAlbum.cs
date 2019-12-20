using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class SubAlbum : IHaveArt
    {
        public List<Song> Songs;
        public string Folder;
        public string Name;
        public string FolderName;
        public Album ParentAlbum;
        public Artist ParentArtist;
        public TagLib.Picture Art;
        public SubAlbum(string folder, Album parent)
        {
            ParentAlbum = parent;
            ParentArtist = parent.Parent;
            Folder = folder;
            FolderName = Path.GetFileName(Folder);
            Name = NameRetriever.GetName(FolderName, correctcase: true);
            Art = ArtRetriever.GetArt(GetArtLocation());
            Songs = new List<Song>();
            foreach (var song in Directory.EnumerateFiles(folder, "*.mp3"))
            {
                Songs.Add(new Song(song, this));
            }
        }

        public string GetArtLocation()
        {
            return Path.Combine(ParentArtist.FolderName, ParentAlbum.FolderName, FolderName);
        }

        public void Save()
        {
            Logger.WriteLine($"SUBALBUM: {Name}");
            Logger.WriteLine($"ART: {GetArtLocation()}");

            string subalbumini = Path.Combine(Folder, "desktop.ini");
            File.Delete(subalbumini);
            File.WriteAllText(subalbumini, "[.ShellClassInfo]\nIconResource = ..\\..\\..\\.music-cache\\art\\" + GetArtLocation() + ".ico, 0");
            File.SetAttributes(subalbumini, FileAttributes.System | FileAttributes.Hidden);

            Logger.TabIn();
            foreach (var song in Songs)
            {
                song.Save();
            }
            Logger.TabOut();
        }
    }
}
