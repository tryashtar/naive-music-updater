using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public interface IHaveArt
    {
        string GetArtLocation();
    }

    public class Library
    {
        public List<Artist> Artists;
        public string Folder;
        public Library(string folder)
        {
            Folder = folder;
            Artists = new List<Artist>();
            foreach (var artist in Directory.EnumerateDirectories(folder))
            {
                if (!Path.GetFileName(artist).StartsWith("."))
                    Artists.Add(new Artist(artist));
            }
        }

        public void Save()
        {
            foreach (var artist in Artists)
            {
                artist.Save();
            }
        }
    }
}
