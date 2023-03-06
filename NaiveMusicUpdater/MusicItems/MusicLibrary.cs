namespace NaiveMusicUpdater;

public class MusicLibrary : MusicFolder
{
    public readonly LibraryConfig LibraryConfig;

    public MusicLibrary(LibraryConfig config) : base(config.LibraryFolder)
    {
        LibraryConfig = config;
        LoadConfigs();
    }

    public void UpdateLibrary()
    {
        if (LibraryConfig.LogFolder != null)
            Logger.Open(Path.Combine(LibraryConfig.LogFolder, DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss") + ".txt"));
        this.Update();
        LibraryConfig.Save();
    }

    protected override void RemoveIcon()
    {
    }

    protected override void SetIcon(string path)
    {
    }
}