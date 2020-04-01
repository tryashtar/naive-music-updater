using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class Artist : IHaveArt
    {
        public List<Album> Albums;
        public string Folder;
        public string FolderName;
        public string Name;
        public TagLib.Picture Art;
        public Artist(string folder)
        {
            Folder = folder;
            FolderName = Path.GetFileName(Folder);
            Name = NameRetriever.GetName(FolderName, correctcase: true);
            Art = ArtRetriever.GetArt(GetArtLocation());
            Albums = new List<Album>();
            foreach (var album in Directory.EnumerateDirectories(folder))
            {
                Albums.Add(new Album(album, this));
            }
        }

        public string GetArtLocation()
        {
            return FolderName;
        }

        public void Save(string cachefolder)
        {
            Logger.WriteLine($"ARTIST: {Name}");
            Logger.WriteLine($"ART: {GetArtLocation()}");

            string artistini = Path.Combine(Folder, "desktop.ini");
            File.Delete(artistini);
            File.WriteAllText(artistini, "[.ShellClassInfo]\nIconResource = ..\\.music-cache\\art\\" + GetArtLocation() + ".ico, 0");
            File.SetAttributes(artistini, FileAttributes.System | FileAttributes.Hidden);

            Logger.TabIn();
            foreach (var album in Albums)
            {
                album.Save(cachefolder);
            }
            Logger.TabOut();
        }
    }
}
