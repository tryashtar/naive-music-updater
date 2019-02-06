using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using NaiveMusicUpdater;

namespace NaiveMusicUpdater
{
    public static class Program
    {
        private static string FolderPath;

        [STAThread]
        private static void Main()
        {
#if DEBUG
            FolderPath = @"D:\Music";
            NaiveSongUpdate(FolderPath);
            Console.ReadLine();
#else
            FolderPath = Directory.GetCurrentDirectory();
            NaiveSongUpdate(FolderPath);
#endif
        }

        private static void NaiveSongUpdate(string folder)
        {
            TagLib.Id3v2.Tag.DefaultVersion = 3;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;

            // create art folder if it's not already there
            string cache = Path.Combine(folder, ".music-cache");
            DirectoryInfo di = Directory.CreateDirectory(cache);
            di.Attributes |= FileAttributes.System | FileAttributes.Hidden;

            string realnamesfile = Path.Combine(cache, "realnames.json");
            var realnames = new Dictionary<string, string>();
            if (File.Exists(realnamesfile))
                realnames = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(realnamesfile));

            NameRetriever.LoadConfig(Path.Combine(cache, "config.json"));
            ArtRetriever.SetArtSource(cache);
            Library library = new Library(folder);
            string logfile = Path.Combine(cache, "logs", DateTime.Now.ToString("yyyy-MM-dd hh_mm_ss") + ".txt");
            using (File.Create(logfile)) ;
            using (StreamWriter logwriter = new StreamWriter(logfile))
            {
                foreach (var message in library.Save())
                {
                    Console.WriteLine(message);
                    logwriter.WriteLine(message);
                }
            }

            Console.WriteLine();
            string sourcesjson = Path.Combine(folder, "sources.json");
            var sources = JObject.Parse(File.ReadAllText(sourcesjson));

            // first add new blank templates
            foreach (var artist in library.Artists)
            {
                var jartist = (JObject)sources[artist.Name];
                if (jartist == null)
                {
                    jartist = new JObject();
                    sources.Add(artist.Name, jartist);
                }
                foreach (var album in artist.Albums)
                {
                    var jalbum = (JObject)jartist[album.Name];
                    if (jalbum == null)
                    {
                        jalbum = new JObject();
                        jartist.Add(album.Name, jalbum);
                    }
                }
            }
            Console.WriteLine("Sources update done");

            // then do scan
            foreach (var jartist in sources)
            {
                var artist = library.Artists.FirstOrDefault(x => x.Name == jartist.Key);
                if (artist == null)
                    Console.WriteLine($"Sources contains artist {jartist.Key} but library doesn't?");
                else
                {
                    foreach (var jalbum in (JObject)jartist.Value)
                    {
                        var album = artist.Albums.FirstOrDefault(x => x.Name == jalbum.Key);
                        if (album == null)
                            Console.WriteLine($"Sources contains album {jartist.Key}/{jalbum.Key} but library doesn't?");
                        else
                        {
                            var songs = album.AllSongs();
                            var extrasongs = (JObject)jalbum.Value["songs"];
                            if (extrasongs == null)
                            {
                                if (jalbum.Value["source"] == null)
                                    Console.WriteLine($"No source or song list for {jartist.Key}/{jalbum.Key}");
                            }
                            else
                            {
                                foreach (var jsong in extrasongs)
                                {
                                    var song = songs.FirstOrDefault(x => ((x.ParentSubAlbum == null ? x.Title : (x.ParentSubAlbum.Name + "/" + x.Title)) == jsong.Key));
                                    if (song == null)
                                        Console.WriteLine($"Sources contains song {jartist.Key}/{jalbum.Key}/{jsong.Key} but library doesn't?");
                                }
                                if (jalbum.Value["source"] == null && songs.Count() > extrasongs.Count)
                                    Console.WriteLine($"Song list for {jartist.Key}/{jalbum.Key} doesn't include all songs (has {extrasongs.Count}, needs {songs.Count()})");
                            }
                        }
                    }
                }
            }
            File.WriteAllText(sourcesjson, sources.ToString());
            Console.WriteLine("Sources scan done");
        }
    }
}
