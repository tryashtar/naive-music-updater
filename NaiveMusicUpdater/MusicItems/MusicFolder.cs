namespace NaiveMusicUpdater;

public class MusicFolder : IMusicItem
{
    private bool HasScanned;

    // full path to folder
    public string Location { get; }

    // configs found directly in this particular folder
    public List<IMusicItemConfig> Configs
    {
        get
        {
            if (_Configs == null) LoadConfigs();
            return _Configs!;
        }
    }

    private List<IMusicItemConfig>? _Configs;

    public MusicFolder? Parent { get; }

    private readonly List<MusicFolder> ChildFolders = new();
    private readonly List<Song> SongList = new();

    public IReadOnlyList<MusicFolder> SubFolders
    {
        get
        {
            if (!HasScanned)
                ScanContents();
            return ChildFolders.AsReadOnly();
        }
    }

    public IReadOnlyList<Song> Songs
    {
        get
        {
            if (!HasScanned)
                ScanContents();
            return SongList.AsReadOnly();
        }
    }

    public IEnumerable<IMusicItem> SubItems => SubFolders.Concat<IMusicItem>(Songs);

    protected MusicFolder(string folder) : this(null, folder)
    {
    }

    private MusicFolder(MusicFolder? parent, string folder)
    {
        Location = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).TrimEnd('.');
        Parent = parent;
    }

    protected void LoadConfigs()
    {
        _Configs = new();
        var path = this.StringPathAfterRoot();
        foreach (var place in RootLibrary.LibraryConfig.ConfigFolders)
        {
            string config = Path.Combine(place, path, "config.yaml");
            if (File.Exists(config))
                Configs.Add(MusicItemConfigFactory.Create(config, this));
        }
    }

    public IEnumerable<Song> GetAllSongs()
    {
        return Songs.Concat(SubFolders.SelectMany(x => x.GetAllSongs()));
    }

    public IEnumerable<IMusicItem> GetAllSubItems()
    {
        return SubItems.Concat(SubFolders.SelectMany(x => x.GetAllSubItems()));
    }

    public string SimpleName => Path.GetFileName(Location);

    public MusicLibrary RootLibrary => (MusicLibrary)this.PathFromRoot().First();

    // icon implementation is very lazy
    // it makes no effort to preserve any fields in these files besides the ones that control icons
    // also .directory files are certainly not ubiquitous among linux file managers
    protected virtual void RemoveIcon()
    {
        if (OperatingSystem.IsWindows())
        {
            string desktop_ini = Path.Combine(Location, "desktop.ini");
            File.Delete(desktop_ini);
        }
        else if (OperatingSystem.IsLinux())
        {
            string directory = Path.Combine(Location, ".directory");
            File.Delete(directory);
        }
    }

    protected virtual void SetIcon(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            string desktop_ini = Path.Combine(Location, "desktop.ini");
            File.WriteAllLines(desktop_ini, new[]
            {
                "[.ShellClassInfo]",
                $"IconResource = {path}, 0"
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            string directory = Path.Combine(Location, ".directory");
            File.WriteAllLines(directory, new[]
            {
                "[Desktop Entry]",
                $"Icon = {path}"
            });
        }
    }

    private void HandleIcon()
    {
        var metadata = this.GetMetadata(MetadataField.Art.Only);
        MusicItemExtensions.HandleArt(metadata, RootLibrary.LibraryConfig);
        var image = metadata.Get(MetadataField.Art);
        if (image.IsBlank)
            RemoveIcon();
        else if (RootLibrary.LibraryConfig.ArtTemplates != null)
        {
            var icon = RootLibrary.LibraryConfig.ArtTemplates.GetIcon(image.AsString().Value);
            SetIcon(Path.GetFullPath(icon));
        }
    }

    public void Update()
    {
        if (RootLibrary.LibraryConfig.ShowUnchanged)
            Logger.WriteLine($"Folder: {SimpleName}", ConsoleColor.Gray);
#if !DEBUG
        HandleIcon();
#endif
        if (RootLibrary.LibraryConfig.ShowUnchanged)
            Logger.TabIn();
        foreach (var child in SubFolders)
        {
            child.Update();
        }

        foreach (var song in Songs)
        {
            song.Update();
        }

        if (RootLibrary.LibraryConfig.ShowUnchanged)
            Logger.TabOut();
    }

    private void ScanContents()
    {
        ChildFolders.Clear();
        var info = new DirectoryInfo(Location);
        foreach (DirectoryInfo dir in info.EnumerateDirectories())
        {
            if (dir.Attributes.HasFlag(FileAttributes.Hidden))
                continue;
            var child = new MusicFolder(this, dir.FullName);
            child.LoadConfigs();
            if (child.Songs.Any() || child.SubFolders.Any())
                ChildFolders.Add(child);
        }

        SongList.Clear();
        foreach (var file in Directory.EnumerateFiles(Location))
        {
            if (RootLibrary.LibraryConfig.IsSongFile(file))
                SongList.Add(new Song(this, file));
        }

        HasScanned = true;
    }

    public override string ToString()
    {
        return String.Join("/", this.PathFromRoot().Select(x => x.SimpleName));
    }
}