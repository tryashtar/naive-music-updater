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
    // to do:
    // if we change art that a song uses (artist/album/subalbum),
    // it won't be applied because the song will be skipped
    // we need a better function for naming arts, and the optimizer to keep track of arts
    // songs whose art was changed recently are not skipped
    public static class Program
    {
        private static string FolderPath;
        private static StreamWriter Writer;

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

        private static void OpenLogFile(string path)
        {
            Writer = new StreamWriter(path);
        }

        private static void CloseLogFile()
        {
            Writer.Close();
        }

        private static void PrintAndLog(string text)
        {
            Console.WriteLine(text);
            Writer.WriteLine(text);
        }

        private static void NaiveSongUpdate(string folder)
        {
            TagLib.Id3v2.Tag.DefaultVersion = 3;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;

            // create art folder if it's not already there
            string cache = Path.Combine(folder, ".music-cache");
            DirectoryInfo di = Directory.CreateDirectory(cache);
            di.Attributes |= FileAttributes.System | FileAttributes.Hidden;

            // set up globals
            NameRetriever.LoadConfig(Path.Combine(cache, "config.json"));
            ArtRetriever.SetArtSource(Path.Combine(cache, "art"), SearchOption.AllDirectories);
            ModifiedOptimizer.LoadCache(Path.Combine(cache, "datecache.json"));

            // prepare to log
            string logfile = Path.Combine(cache, "logs", DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss") + ".txt");
            using (File.Create(logfile)) ;
            OpenLogFile(logfile);

            // scan and save library
            Library library = new Library(folder);
            foreach (var message in library.Save())
            {
                PrintAndLog(message);
            }

            // persist globals
            ArtRetriever.MarkAllArtRead();
            ModifiedOptimizer.SaveCache();

            // prepare to scan sources
            PrintAndLog("");
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
            PrintAndLog("Sources update done");

            // then do scan
            // to do:
            // for each album, notice all songs that aren't in "songs" JSON
            // save "source" with this list
            // if next time, list doesn't match what was saved next time, any new songs
            // clearly came from a different source, so ask user to:
            // (1) write source of new songs in "songs" JSON; or
            // (2) confirm this new song exists in source (resave if so)
            // the intent is to make sure all songs in the entire library are accounted for
            foreach (var jartist in sources)
            {
                var artist = library.Artists.FirstOrDefault(x => x.Name == jartist.Key);
                if (artist == null)
                    PrintAndLog($"Sources contains artist {jartist.Key} but library doesn't?");
                else
                {
                    foreach (var jalbum in (JObject)jartist.Value)
                    {
                        var album = artist.Albums.FirstOrDefault(x => x.Name == jalbum.Key);
                        if (album == null)
                            PrintAndLog($"Sources contains album {jartist.Key}/{jalbum.Key} but library doesn't?");
                        else
                        {
                            var songs = album.AllSongs();
                            var extrasongs = (JObject)jalbum.Value["songs"];
                            if (extrasongs == null)
                            {
                                if (jalbum.Value["source"] == null)
                                    PrintAndLog($"No source or song list for {jartist.Key}/{jalbum.Key}");
                            }
                            else
                            {
                                foreach (var jsong in extrasongs)
                                {
                                    var song = songs.FirstOrDefault(x => ((x.ParentSubAlbum == null ? x.Filename : (x.ParentSubAlbum.Name + "/" + x.Filename)) == jsong.Key));
                                    if (song == null)
                                        PrintAndLog($"Sources contains song {jartist.Key}/{jalbum.Key}/{jsong.Key} but library doesn't?");
                                }
                                if (jalbum.Value["source"] == null && songs.Count() > extrasongs.Count)
                                    PrintAndLog($"Song list for {jartist.Key}/{jalbum.Key} doesn't include all songs (has {extrasongs.Count}, needs {songs.Count()})");
                            }
                        }
                    }
                }
            }
            File.WriteAllText(sourcesjson, sources.ToString());
            PrintAndLog("Sources scan done");
            CloseLogFile();
        }
    }
}
