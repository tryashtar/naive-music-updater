using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NaiveMusicUpdater;

public class ExportConfig<T> where T : struct, Enum
{
    public readonly string ExternalFolder;
    public readonly T[] Priority;
    public readonly Dictionary<T, ExportOption> Decisions;

    public ExportConfig(string externalFolder, T[] priority, Dictionary<T, ExportOption> decisions)
    {
        ExternalFolder = externalFolder;
        Priority = priority;
        Decisions = decisions;
    }
}

public static class ExportConfigExtensions
{
    public static Lyrics? BestLyrics(this ExportConfig<LyricsType> config, TagLib.File file, string path)
    {
        foreach (var item in config.Priority)
        {
            var lyrics = GetLyrics(item, file, path);
            if (lyrics != null)
                return lyrics;
        }

        return null;
    }

    public static Lyrics? GetLyrics(LyricsType type, TagLib.File file, string path)
    {
        return type switch
        {
            LyricsType.SimpleEmbedded => LyricsIO.FromFile(file, LyricTypes.Simple),
            LyricsType.SyncedEmbedded => LyricsIO.FromFile(file, LyricTypes.Synced),
            LyricsType.RichEmbedded => LyricsIO.FromFile(file, LyricTypes.Rich),
            LyricsType.SyncedFile => SyncedFileLyrics(path, file.Properties.Duration),
            LyricsType.RichFile => RichFileLyrics(path),
            _ => null
        };
    }

    public static ChapterCollection? BestChapters(this ExportConfig<ChaptersType> config, TagLib.File file, string path)
    {
        foreach (var item in config.Priority)
        {
            var chap = GetChapters(item, file, path);
            if (chap != null)
                return chap;
        }

        return null;
    }

    public static ChapterCollection? GetChapters(ChaptersType type, TagLib.File file, string path)
    {
        return type switch
        {
            ChaptersType.SimpleEmbedded => ChaptersIO.FromFile(file, ChapterTypes.Simple),
            ChaptersType.RichEmbedded => ChaptersIO.FromFile(file, ChapterTypes.Rich),
            ChaptersType.SimpleFile => SimpleFileChapters(path, file.Properties.Duration),
            ChaptersType.RichFile => RichFileChapters(path),
            _ => null
        };
    }

    public static bool SetChapters(ChapterCollection? chapters, ChaptersType? type, TagLib.File file, string path)
    {
        switch (type)
        {
            case ChaptersType.SimpleEmbedded:
                return ChaptersIO.ToFile(file, chapters, ChapterTypes.Simple);
            case ChaptersType.RichEmbedded:
                return ChaptersIO.ToFile(file, chapters, ChapterTypes.Rich);
            case ChaptersType.SimpleFile:
                path += ".chp";
                if (chapters == null)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        return true;
                    }

                    return false;
                }
                else
                {
                    var chp = chapters.ToChp();
                    if (File.Exists(path))
                    {
                        var existing = File.ReadLines(path);
                        if (existing.SequenceEqual(chp))
                            return false;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllLines(path, chp);
                    return true;
                }
            case ChaptersType.RichFile:
                path += ".chp.json";
                if (chapters == null)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        return true;
                    }

                    return false;
                }
                else
                {
                    var json = ChaptersIO.ToJson(chapters);
                    var existing = ReadJson(path);
                    if (existing != null && (JToken.DeepEquals(json, existing) || json.ToString() == existing.ToString()))
                        return false;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using var stream = File.CreateText(path);
                    using var writer = new JsonTextWriter(stream) { Formatting = Formatting.Indented };
                    json.WriteTo(writer);
                    return true;
                }
        }

        return false;
    }

    public static bool SetLyrics(Lyrics? lyrics, LyricsType? type, TagLib.File file, string path)
    {
        switch (type)
        {
            case LyricsType.SimpleEmbedded:
                return LyricsIO.ToFile(file, lyrics, LyricTypes.Simple);
            case LyricsType.SyncedEmbedded:
                return LyricsIO.ToFile(file, lyrics, LyricTypes.Synced);
            case LyricsType.RichEmbedded:
                return LyricsIO.ToFile(file, lyrics, LyricTypes.Rich);
            case LyricsType.SyncedFile:
                path += ".lrc";
                if (lyrics == null)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        return true;
                    }

                    return false;
                }
                else
                {
                    var lrc = lyrics.ToLrc();
                    if (File.Exists(path))
                    {
                        var existing = File.ReadLines(path);
                        if (existing.SequenceEqual(lrc))
                            return false;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllLines(path, lrc);
                    return true;
                }

                break;
            case LyricsType.RichFile:
                path += ".lrc.json";
                if (lyrics == null)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        return true;
                    }

                    return false;
                }
                else
                {
                    var json = LyricsIO.ToJson(lyrics);
                    var existing = ReadJson(path);
                    if (existing != null && (JToken.DeepEquals(json, existing) || json.ToString() == existing.ToString()))
                        return false;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using var stream = File.CreateText(path);
                    using var writer = new JsonTextWriter(stream) { Formatting = Formatting.Indented };
                    json.WriteTo(writer);
                    return true;
                }
        }

        return false;
    }

    private static Lyrics? SyncedFileLyrics(string path, TimeSpan duration)
    {
        path += ".lrc";
        if (File.Exists(path))
            return LyricsIO.FromLrc(File.ReadLines(path), duration);
        return null;
    }

    private static Lyrics? RichFileLyrics(string path)
    {
        path += ".lrc.json";
        var json = ReadJson(path);
        if (json == null)
            return null;
        return LyricsIO.FromJson(json);
    }

    private static ChapterCollection? SimpleFileChapters(string path, TimeSpan duration)
    {
        path += ".chp";
        if (File.Exists(path))
            return ChaptersIO.FromChp(File.ReadLines(path), duration);
        return null;
    }

    private static ChapterCollection? RichFileChapters(string path)
    {
        path += ".chp.json";
        var json = ReadJson(path);
        if (json == null)
            return null;
        return ChaptersIO.FromJson(json);
    }

    private static JObject? ReadJson(string path)
    {
        if (!File.Exists(path))
            return null;
        using var file = File.OpenText(path);
        using var reader = new JsonTextReader(file);
        return JObject.Load(reader);
    }

    private static void WriteJson(JObject json, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using var stream = File.CreateText(path);
        using var writer = new JsonTextWriter(stream) { Formatting = Formatting.Indented };
        json.WriteTo(writer);
    }
}

public enum LyricsType
{
    SimpleEmbedded,
    SyncedEmbedded,
    RichEmbedded,
    SyncedFile,
    RichFile
}

public enum ChaptersType
{
    SimpleEmbedded,
    RichEmbedded,
    SimpleFile,
    RichFile
}

public enum ExportOption
{
    Ignore,
    Remove,
    Replace
}