using System.Runtime.InteropServices;

namespace NaiveMusicUpdater;

public class MusicFolder : IMusicItem
{
    private bool HasScanned = false;
    public string Location { get; private set; }
    public IMusicItemConfig LocalConfig { get; protected set; }
    public virtual LibraryCache GlobalCache => _Parent?.GlobalCache ?? throw new NullReferenceException();
    protected readonly MusicFolder? _Parent;
    public MusicFolder? Parent => _Parent;
    protected readonly List<MusicFolder> ChildFolders = new();
    protected readonly List<Song> SongList = new();
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
    public MusicFolder(string folder) : this(null, folder)
    { }

    private MusicFolder(MusicFolder? parent, string folder)
    {
        Location = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).TrimEnd('.');
        _Parent = parent;
        string config = Path.Combine(folder, "config.yaml");
        if (File.Exists(config))
            LocalConfig = MusicItemConfigFactory.Create(config, this);
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

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    static extern void SHChangeNotify(int wEventId, int uFlags, [MarshalAs(UnmanagedType.LPWStr)] string dwItem1, [MarshalAs(UnmanagedType.LPWStr)] string? dwItem2);

    public void Update()
    {
        Logger.WriteLine($"Folder: {SimpleName}", ConsoleColor.Gray);

        //var metadata = MusicItemUtils.GetMetadata(this, MetadataField.All);
#if !DEBUG
        //var art = GlobalCache.GetArtPathFor(this);
        //string desktop_ini = Path.Combine(Location, "desktop.ini");
        //File.Delete(desktop_ini);
        //if (art != null)
        //{
        //    ArtCache.LoadAndMakeIcon(art);
        //    File.WriteAllText(desktop_ini, $"[.ShellClassInfo]\nIconResource = {Path.ChangeExtension(Path.GetRelativePath(Location, art), ".ico")}, 0");
        //    File.SetAttributes(desktop_ini, FileAttributes.System | FileAttributes.Hidden);
        //    SHChangeNotify(0x08000000, 0x0005 | 0x2000, Location, null);
        //}
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
            if (child.Songs.Any() || child.SubFolders.Any())
                ChildFolders.Add(child);
        }
        SongList.Clear();
        foreach (var file in Directory.EnumerateFiles(Location))
        {
            if (GlobalCache.Config.IsSongFile(file))
                SongList.Add(new Song(this, file));
        }
        HasScanned = true;
    }

    public CheckSelectorResults CheckSelectors()
    {
        var answer = new CheckSelectorResults();
        if (LocalConfig != null)
        {
            var results = LocalConfig.CheckSelectors();
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
