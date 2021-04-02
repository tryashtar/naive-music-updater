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
            using (TagLib.File file = TagLib.File.Create(Location))
            {
                bool success = true;
                var path = Util.StringPathAfterRoot(this);
                var art = GlobalCache.GetArtPathFor(this);
                var modifier = new TagModifier(file.Tag, GlobalCache);
                modifier.UpdateMetadata(metadata);
                modifier.UpdateArt(art);
                modifier.WriteLyrics(path);
                modifier.WipeUselessProperties();
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
                if (success)
                {
#if !DEBUG
                    GlobalCache.Config.NormalizeAudio(this);
                    GlobalCache.MarkUpdatedRecently(this);
#endif
                }
            }
        }

        private string Resize(string thing, int size)
        {
            if (thing == null)
                return null;
            return TagLib.Id3v1.Tag.DefaultStringHandler.Render(thing).Resize(size).ToString().Trim().TrimEnd('\0');
        }

        public string SimpleName => Path.GetFileNameWithoutExtension(this.Location);

        public IEnumerable<IMusicItem> PathFromRoot() => MusicItemUtils.PathFromRoot(this);
        public MusicLibrary RootLibrary => (MusicLibrary)PathFromRoot().First();
    }
}
