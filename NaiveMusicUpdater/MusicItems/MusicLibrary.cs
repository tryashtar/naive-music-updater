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
        this.Update();
        LibraryConfig.Save();
    }

    // the root library folder is just wherever the user wants it
    // it's not appropriate to replace its icon
    protected override void RemoveIcon()
    {
    }

    protected override void SetIcon(string path)
    {
    }
}