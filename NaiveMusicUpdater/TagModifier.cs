using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NaiveMusicUpdater;

public class TagModifier
{
    public bool HasChanged { get; private set; }
    private readonly TagLib.File TagFile;
    private readonly LibraryConfig Config;

    public TagModifier(TagLib.File file, LibraryConfig config)
    {
        TagFile = file;
        Config = config;
    }

    public void UpdateMetadata(Metadata metadata)
    {
        var interop = TagInteropFactory.GetDynamicInterop(TagFile.Tag, Config);
        if (interop.Changed)
            Logger.WriteLine("Change detected from creating interop!", ConsoleColor.Red);
        foreach (var (field, value) in metadata.SavedFields)
        {
            interop.Set(field, value);
        }

        interop.Clean();

        if (interop.Changed)
            HasChanged = true;
    }

    public void WriteLyrics(string location)
    {
        if (Config.LyricsConfig == null)
            return;

        location = Path.Combine(Config.LyricsConfig.ExternalFolder, location);
        var best = Config.LyricsConfig.BestLyrics(TagFile, location);
        foreach (var (type, action) in Config.LyricsConfig.Decisions)
        {
            if (action == ExportOption.Ignore)
                continue;
            var write = action == ExportOption.Remove ? null : best;
            var old = ExportConfigExtensions.GetLyrics(type, TagFile, location);
            if (ExportConfigExtensions.SetLyrics(write, type, TagFile, location))
            {
                if (type is (LyricsType.RichEmbedded or LyricsType.SyncedEmbedded or LyricsType.SimpleEmbedded))
                    HasChanged = true;
                Logger.WriteLine($"Replacing lyrics at {type}:");
                Logger.TabIn();
                Logger.WriteLine(old?.ToString() ?? "(blank)");
                Logger.WriteLine(best?.ToString() ?? "(blank)");
                Logger.TabOut();
            }
        }
    }

    public void WriteChapters(string location)
    {
        if (Config.ChaptersConfig == null)
            return;

        location = Path.Combine(Config.ChaptersConfig.ExternalFolder, location);
        var best = Config.ChaptersConfig.BestChapters(TagFile, location);
        foreach (var (type, action) in Config.ChaptersConfig.Decisions)
        {
            if (action == ExportOption.Ignore)
                continue;
            var write = action == ExportOption.Remove ? null : best;
            var old = ExportConfigExtensions.GetChapters(type, TagFile, location);
            if (ExportConfigExtensions.SetChapters(write, type, TagFile, location))
            {
                if (type is (ChaptersType.RichEmbedded or ChaptersType.SimpleEmbedded))
                    HasChanged = true;
                Logger.WriteLine($"Replacing chapters at {type}:");
                Logger.TabIn();
                Logger.WriteLine(old?.ToString() ?? "(blank)");
                Logger.WriteLine(best?.ToString() ?? "(blank)");
                Logger.TabOut();
            }
        }
    }
}