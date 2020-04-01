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
        [STAThread]
        private static void Main()
        {
            string FolderPath;
#if DEBUG
            FolderPath = @"D:\Music";
#else
            FolderPath = Directory.GetCurrentDirectory();
#endif
            var library = NaiveSongUpdate(FolderPath);
            Logger.WriteLine("");
            SourcesUpdate(library);
            Logger.Close();
#if DEBUG
            Console.ReadLine();
#endif
        }

        private static Library NaiveSongUpdate(string folder)
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
            Logger.Open(logfile);

            // scan and save library
            var library = new Library(folder);
            library.Save(cache);

            // persist globals
            ArtRetriever.MarkAllArtRead();
            ModifiedOptimizer.SaveCache();
            return library;
        }

        private static void SourcesUpdate(Library library)
        {
            // prepare to scan sources
            string sourcesjson = Path.Combine(library.Folder, "sources.json");
            var sources = JObject.Parse(File.ReadAllText(sourcesjson));

            // first add new blank templates
            foreach (var artist in library.Artists)
            {
                var jartist = (JObject)sources[artist.FolderName];
                if (jartist == null)
                {
                    jartist = new JObject();
                    sources.Add(artist.FolderName, jartist);
                }
                foreach (var album in artist.Albums)
                {
                    var jalbum = (JObject)jartist[album.FolderName];
                    if (jalbum == null)
                    {
                        jalbum = new JObject();
                        jartist.Add(album.FolderName, jalbum);
                    }
                }
            }
            Logger.WriteLine("Sources update done");

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
                var artist = library.Artists.FirstOrDefault(x => x.FolderName == jartist.Key);
                if (artist == null)
                    Logger.WriteLine($"Sources contains artist {jartist.Key} but library doesn't?");
                else
                {
                    foreach (var jalbum in (JObject)jartist.Value)
                    {
                        var album = artist.Albums.FirstOrDefault(x => x.FolderName == jalbum.Key);
                        if (album == null)
                            Logger.WriteLine($"Sources contains album {jartist.Key}/{jalbum.Key} but library doesn't?");
                        else
                        {
                            var songs = album.AllSongs().Select(x => x.SubFilename).ToList();
                            foreach (var source in (JObject)jalbum.Value)
                            {
                                if (source.Key == "")
                                    continue;
                                IEnumerable<string> sourced;
                                if (source.Value is JArray j)
                                    sourced = j.ToObject<string[]>();
                                else
                                    sourced = new string[] { (string)source.Value };
                                foreach (string song in sourced)
                                {
                                    if (songs.Contains(song))
                                        songs.Remove(song);
                                    else
                                        Logger.WriteLine($"Sources contains song {jartist.Key}/{jalbum.Key}/{song} but library doesn't?");
                                }
                            }
                            if (songs.Any())
                                jalbum.Value[""] = new JArray();
                            foreach (var song in songs)
                            {
                                Logger.WriteLine($"No source for {jartist.Key}/{jalbum.Key}/{song}");
                                ((JArray)jalbum.Value[""]).Add(song);
                            }
                        }
                    }
                }
            }
            File.WriteAllText(sourcesjson, sources.ToString());
            Logger.WriteLine("Sources scan done");
        }
    }
}
