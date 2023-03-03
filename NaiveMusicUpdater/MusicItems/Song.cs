namespace NaiveMusicUpdater;

public class Song : IMusicItem
{
    public string Location { get; }
    protected readonly MusicFolder _Parent;
    public MusicFolder Parent => _Parent;
    public IMusicItemConfig[] Configs => Array.Empty<IMusicItemConfig>();

    public Song(MusicFolder parent, string file)
    {
        _Parent = parent;
        Location = file;
    }
    
    public void Update()
    {
        Logger.WriteLine($"Song: {SimpleName}", ConsoleColor.Gray);
        if (!RootLibrary.LibraryConfig.Cache.NeedsUpdate(this))
            return;
        Logger.WriteLine($"(checking)");
        var metadata = this.GetMetadata(MetadataField.All);
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
            try { file.Save(); }
            catch (IOException ex)
            {
                Logger.WriteLine($"Save failed because {ex.Message}! Skipping...", ConsoleColor.Red);
                RootLibrary.LibraryConfig.Cache.MarkNeedsUpdateNextTime(this);
                success = false;
            }
        }
        if (success)
            RootLibrary.LibraryConfig.Cache.MarkUpdatedRecently(this);
#else
        if (modifier.HasChanged)
            Logger.WriteLine("Changed!");
#endif
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