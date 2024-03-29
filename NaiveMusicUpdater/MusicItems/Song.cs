namespace NaiveMusicUpdater;

public class Song : IMusicItem
{
    // full path to file
    public string Location { get; }
    public MusicFolder Parent { get; }

    // songs are not folders and therefore can't have config files inside of them
    // of course, configs in parent folders can still apply to particular songs
    private static readonly List<IMusicItemConfig> _Empty = new();
    public List<IMusicItemConfig> Configs => _Empty;

    public Song(MusicFolder parent, string file)
    {
        Parent = parent;
        Location = file;
    }

    public void Update()
    {
        if (RootLibrary.LibraryConfig.ShowUnchanged)
            Logger.WriteLine($"Song: {SimpleName}", ConsoleColor.Gray);
        var set_exports = RootLibrary.LibraryConfig.FieldExports.Where(x => !x.embedded).Select(x => x.export).ToList();
        var embed_exports = RootLibrary.LibraryConfig.FieldExports.Where(x => x.embedded).Select(x => x.export)
            .ToList();
        Metadata? metadata = null;
        if (set_exports.Count > 0)
        {
            metadata = this.GetMetadata(MetadataField.All);
            MusicItemExtensions.HandleArt(metadata, RootLibrary.LibraryConfig);
            foreach (var export in set_exports)
            {
                export.Remember(this, metadata);
            }
        }

        if (RootLibrary.LibraryConfig.Cache.NeedsUpdate(this))
        {
            if (RootLibrary.LibraryConfig.ShowUnchanged)
            {
                Logger.TabIn();
                Logger.WriteLine($"(checking)");
            }
            else
            {
                Logger.WriteLine($"Song: {this.StringPathAfterRoot()}", ConsoleColor.Gray);
                Logger.TabIn();
            }

            if (metadata == null)
            {
                metadata = this.GetMetadata(MetadataField.All);
                MusicItemExtensions.HandleArt(metadata, RootLibrary.LibraryConfig);
            }
#if !DEBUG
            bool reload_file = true;
            using var replay_file = TagLib.File.Create(Location);
            bool needs_replaygain = !HasReplayGain(replay_file);
            if (needs_replaygain)
            {
                Logger.WriteLine($"Normalizing audio with ReplayGain");
                RootLibrary.LibraryConfig.NormalizeAudio(this);
                replay_file.Dispose();
            }
            else
                reload_file = false;
    
            using var file = reload_file ? TagLib.File.Create(Location) : replay_file;
#else
            using var file = TagLib.File.Create(Location);
#endif
            var path = this.StringPathAfterRoot();
            var modifier = new TagModifier(file, RootLibrary.LibraryConfig);
            modifier.UpdateMetadata(metadata);
            modifier.WriteLyrics(path);
            modifier.WriteChapters(path);

#if !DEBUG
            bool success = true;
            if (modifier.HasChanged)
            {
                Logger.WriteLine("Saving...", ConsoleColor.Green);
                try
                {
                    file.Save();
                }
                catch (IOException ex)
                {
                    Logger.WriteLine($"Save failed because {ex.Message}! Skipping...", ConsoleColor.Red);
                    success = false;
                }
            }
    
            if (success)
                RootLibrary.LibraryConfig.Cache.MarkUpdated(this);
#else
            if (modifier.HasChanged)
            {
                if (RootLibrary.LibraryConfig.ShowUnchanged)
                    Logger.WriteLine("Changed!");
            }
#endif
            Logger.TabOut();
        }

        if (embed_exports.Count > 0)
        {
            var meta = this.GetEmbeddedMetadata(MetadataField.All);
            foreach (var export in embed_exports)
            {
                export.Remember(this, meta);
            }
        }
    }

    private bool HasReplayGain(TagLib.File file)
    {
        const string TRACK_GAIN = "REPLAYGAIN_TRACK_GAIN";
        var id3v2 = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2);
        if (id3v2 != null)
        {
            var frames = id3v2.GetFrames<RelativeVolumeFrame>();
            if (frames.Any())
                return true;
        }

        var ape = (TagLib.Ape.Tag)file.GetTag(TagTypes.Ape);
        if (ape != null)
        {
            if (ape.HasItem(TRACK_GAIN))
                return true;
        }

        var ogg = (TagLib.Ogg.XiphComment)file.GetTag(TagTypes.Xiph);
        if (ogg != null)
        {
            var gain = ogg.GetFirstField(TRACK_GAIN);
            if (gain != null)
                return true;
        }

        return false;
    }

    public string SimpleName => Path.GetFileNameWithoutExtension(this.Location);

    public MusicLibrary RootLibrary => (MusicLibrary)this.PathFromRoot().First();

    public Metadata GetEmbeddedMetadata(Predicate<MetadataField> desired)
    {
        using var file = TagLib.File.Create(Location);
        var interop = TagInteropFactory.GetDynamicInterop(file.Tag, RootLibrary.LibraryConfig);
        return interop.GetFullMetadata(desired);
    }
}