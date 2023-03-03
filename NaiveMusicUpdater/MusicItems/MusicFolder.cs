namespace NaiveMusicUpdater;

public class MusicFolder : IMusicItem
{
    private bool HasScanned = false;
    public string Location { get; }
    public IMusicItemConfig[] Configs { get; private set; }
    public virtual LibraryConfig GlobalConfig => _Parent?.GlobalConfig ?? throw new NullReferenceException();
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
        var path = ((IMusicItem)this).StringPathAfterRoot();
        foreach (var place in GlobalConfig.ConfigFolders)
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

    public IEnumerable<IMusicItem> PathFromRoot() => MusicItemUtils.PathFromRoot(this);
    public MusicLibrary RootLibrary => (MusicLibrary)PathFromRoot().First();

    public void Update()
    {
        Logger.WriteLine($"Folder: {SimpleName}", ConsoleColor.Gray);
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
            if (GlobalConfig.IsSongFile(file))
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
        return String.Join("/", MusicItemUtils.PathFromRoot(this).Select(x => x.SimpleName));
    }
}