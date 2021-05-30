using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using TagLib.Id3v2;
using TagLib.Matroska;
using File = System.IO.File;

namespace NaiveMusicUpdater
{
    public class Song : IMusicItem
    {
        public string Location { get; private set; }
        protected readonly MusicFolder _Parent;
        public MusicFolder Parent => _Parent;
        public MusicItemConfig LocalConfig => null;
        public LibraryCache GlobalCache => _Parent.GlobalCache;
        public Song(MusicFolder parent, string file)
        {
            _Parent = parent;
            Location = file;
        }

#if DEBUG
        private static string Breakpoint;
        static Song()
        {
            if (File.Exists("break.txt"))
                Breakpoint = File.ReadAllText("break.txt").ToLower().Replace("\n", "").Replace("\r", "");
        }
#endif

        public void Update()
        {
            Logger.WriteLine($"Song: {SimpleName}");
#if !DEBUG
            if (!GlobalCache.NeedsUpdate(this))
                return;
#endif
#if DEBUG
            if (Breakpoint != null && !SimpleName.ToLower().Contains(Breakpoint))
                return;
#endif
            Logger.WriteLine($"(checking)");
            var metadata = MusicItemUtils.GetMetadata(this, MetadataField.All);
#if !DEBUG
            bool reload_file = true;
            using var replay_file = TagLib.File.Create(Location);
            bool needs_replaygain = !HasReplayGain(replay_file);
            if (needs_replaygain)
            {
                Logger.WriteLine($"Normalizing audio with ReplayGain");
                GlobalCache.Config.NormalizeAudio(this);
                replay_file.Dispose();
            }
            else
                reload_file = false;
            using var file = reload_file ? TagLib.File.Create(Location) : replay_file;
#else
            using var file = TagLib.File.Create(Location);
#endif
            var path = Util.StringPathAfterRoot(this);
            var art = GlobalCache.GetArtPathFor(this);
            var modifier = new TagModifier(file, GlobalCache);
            modifier.UpdateMetadata(metadata);
            modifier.UpdateArt(art);
            modifier.WriteLyrics(path);
            bool success = true;
            if (modifier.HasChanged)
            {
                Logger.WriteLine("Saving...");
                try { file.Save(); }
                catch (IOException ex)
                {
                    Logger.WriteLine($"Save failed because {ex.Message}! Skipping...");
                    GlobalCache.MarkNeedsUpdateNextTime(this);
                    success = false;
                }
            }
#if !DEBUG
            if (success)
                GlobalCache.MarkUpdatedRecently(this);
#endif
        }

        private bool HasReplayGain(TagLib.File file)
        {
            const string TRACK_GAIN = "REPLAYGAIN_TRACK_GAIN";
            var ape = (TagLib.Ape.Tag)file.GetTag(TagTypes.Ape);
            if (ape != null)
                return ape.HasItem(TRACK_GAIN);
            else
            {
                var ogg = (TagLib.Ogg.XiphComment)file.GetTag(TagTypes.Xiph);
                if (ogg != null)
                {
                    var gain = ogg.GetFirstField(TRACK_GAIN);
                    if (gain != null)
                        return true;
                }
            }
            return false;
        }

        public string SimpleName => Path.GetFileNameWithoutExtension(this.Location);

        public IEnumerable<IMusicItem> PathFromRoot() => MusicItemUtils.PathFromRoot(this);
        public MusicLibrary RootLibrary => (MusicLibrary)PathFromRoot().First();
    }
}
