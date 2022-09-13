namespace NaiveMusicUpdater;

public class Song : IMusicItem
{
    public string Location { get; private set; }
    protected readonly MusicFolder _Parent;
    public MusicFolder Parent => _Parent;
    public IMusicItemConfig? LocalConfig => null;
    public LibraryCache GlobalCache => _Parent.GlobalCache;
    public Song(MusicFolder parent, string file)
    {
        _Parent = parent;
        Location = file;
    }

#if DEBUG
    private static readonly string? Breakpoint;
    static Song()
    {
        if (File.Exists("break.txt"))
            Breakpoint = File.ReadAllText("break.txt").ToLower().Replace("\n", "").Replace("\r", "");
    }
#endif

    public void Update()
    {
        Logger.WriteLine($"Song: {SimpleName}", ConsoleColor.Gray);
#if !DEBUG
        if (!GlobalCache.NeedsUpdate(this))
            return;
#endif
#if DEBUG
        if (Breakpoint != null && !SimpleName.ToLower().Contains(Breakpoint) && !String.Join('/', PathFromRoot().Select(x => x.SimpleName)).ToLower().Contains(Breakpoint))
            return;
#endif
        Logger.WriteLine($"(checking)");
        var metadata = MusicItemUtils.GetMetadata(this, MetadataField.All);
#if !DEBUG
        bool reload_file = true;
        using var replay_file = TagLib.File.Create(Location);
        var apetag = replay_file.GetTag(TagTypes.Ape);
        if (apetag != null)
        {
            Logger.WriteLine("Removing ape tag");
            replay_file.RemoveTags(TagTypes.Ape);
        }
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
        modifier.WriteChapters(path);

#if !DEBUG
        bool success = true;
        if (modifier.HasChanged || apetag != null)
        {
            Logger.WriteLine("Saving...", ConsoleColor.Green);
            try { file.Save(); }
            catch (IOException ex)
            {
                Logger.WriteLine($"Save failed because {ex.Message}! Skipping...", ConsoleColor.Red);
                GlobalCache.MarkNeedsUpdateNextTime(this);
                success = false;
            }
        }
        if (success)
            GlobalCache.MarkUpdatedRecently(this);
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

    public IEnumerable<IMusicItem> PathFromRoot() => MusicItemUtils.PathFromRoot(this);
    public MusicLibrary RootLibrary => (MusicLibrary)PathFromRoot().First();

    public Metadata GetEmbeddedMetadata(Predicate<MetadataField> desired)
    {
        using var file = TagLib.File.Create(Location);
        var interop = TagInteropFactory.GetDynamicInterop(file.Tag, GlobalCache.Config);
        return interop.GetFullMetadata(desired);
    }
}
