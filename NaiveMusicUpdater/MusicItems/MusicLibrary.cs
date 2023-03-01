namespace NaiveMusicUpdater;

public class MusicLibrary : MusicFolder
{
    protected readonly LibraryConfig LibraryConfig;
    public override LibraryConfig GlobalConfig => LibraryConfig;
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
        LibraryConfig.Cache.Save();
    }
}
