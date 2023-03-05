namespace NaiveMusicUpdater;

public class MusicFolder : IMusicItem
{
    private bool HasScanned = false;
    public string Location { get; }
    public IMusicItemConfig[] Configs { get; private set; }
    private readonly MusicFolder? _Parent;
    public MusicFolder? Parent => _Parent;
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
        _Parent = parent;
    }

    protected void LoadConfigs()
    {
        var configs = new List<IMusicItemConfig>();
        var path = this.StringPathAfterRoot();
        foreach (var place in RootLibrary.LibraryConfig.ConfigFolders)
        {
            string config = Path.Combine(place, path, "config.yaml");
            if (File.Exists(config))
                configs.Add(MusicItemConfigFactory.Create(config, this));
        }

        Configs = configs.ToArray();
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
        var image = this.GetMetadata(MetadataField.Art.Only).Get(MetadataField.Art);
        if (!image.IsBlank && RootLibrary.LibraryConfig.ArtTemplates != null)
        {
            var path = RootLibrary.LibraryConfig.ArtTemplates.FirstArt(image.AsList().Values).path;
            if (path == null)
                RemoveIcon();
            else
            {
                var icon = RootLibrary.LibraryConfig.ArtTemplates.GetIcon(path);
                if (icon == null)
                    RemoveIcon();
                else
                    SetIcon(Path.GetFullPath(icon));
            }
        }
    }

    public void Update()
    {
        Logger.WriteLine($"Folder: {SimpleName}", ConsoleColor.Gray);
#if !DEBUG
        HandleIcon();
#endif

        Logger.TabIn();
        foreach (var child in SubFolders)
        {
            child.Update();
        }

        foreach (var song in Songs)
        {
            song.Update();
        }

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

    public CheckSelectorResults CheckSelectors()
    {
        var answer = new CheckSelectorResults();
        foreach (var config in Configs)
        {
            var results = config.CheckSelectors();
            if (results.UnusedSelectors.Any())
            {
                Logger.WriteLine($"{this} has unused selectors:");
                Logger.TabIn();
                foreach (var unused in results.UnusedSelectors)
                {
                    Logger.WriteLine(unused.ToString());
                }

                Logger.TabOut();
            }

            if (results.UnselectedItems.Any())
            {
                Logger.WriteLine($"{this} has unselected items:");
                Logger.TabIn();
                foreach (var unselected in results.UnselectedItems)
                {
                    Logger.WriteLine(unselected.SimpleName);
                }

                Logger.TabOut();
            }

            answer.AddResults(results);
        }

        foreach (var item in SubFolders)
        {
            answer.AddResults(item.CheckSelectors());
        }

        return answer;
    }

    public override string ToString()
    {
        return String.Join("/", this.PathFromRoot().Select(x => x.SimpleName));
    }
}