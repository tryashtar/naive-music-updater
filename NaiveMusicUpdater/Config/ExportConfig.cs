using System.Text.Json;

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
        return config.Priority.Select(item => GetLyrics(item, file, path)).FirstOrDefault(x => x != null);
    }

    public static Lyrics? GetLyrics(LyricsType type, TagLib.File file, string path)
    {
        return type switch
        {
            LyricsType.SimpleEmbedded => LyricsIO.FromFile(file, LyricTypes.Simple),
            LyricsType.SyncedEmbedded => LyricsIO.FromFile(file, LyricTypes.Synced),
            LyricsType.RichEmbedded => LyricsIO.FromFile(file, LyricTypes.Rich),
            LyricsType.CustomEmbedded => CustomLyrics(file, file.Properties.Duration),
            LyricsType.SyncedFile => SyncedFileLyrics(path, file.Properties.Duration),
            LyricsType.RichFile => RichFileLyrics(path),
            _ => null
        };
    }

    private static Lyrics? CustomLyrics(TagLib.File file, TimeSpan duration)
    {
        var tag = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2);
        if (tag == null)
            return null;
        foreach (var frame in tag.GetFrames<UserTextInformationFrame>()
                     .Where(x => x.Description == "LYRICS"))
        {
            if (frame.Text.Length > 0)
            {
                var lyrics = StringUtils.SplitLines(frame.Text[0]).ToArray();
                if (LyricsIO.TryFromLrc(lyrics, duration, out var result))
                    return result;
            }
        }

        return null;
    }

    public static bool SetLyrics(Lyrics? lyrics, LyricsType type, TagLib.File file, string path)
    {
        switch (type)
        {
            case LyricsType.SimpleEmbedded:
                return LyricsIO.ToFile(file, lyrics, LyricTypes.Simple);
            case LyricsType.SyncedEmbedded:
                return LyricsIO.ToFile(file, lyrics, LyricTypes.Synced);
            case LyricsType.RichEmbedded:
                return LyricsIO.ToFile(file, lyrics, LyricTypes.Rich);
            case LyricsType.CustomEmbedded:
                var tag = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2);
                if (tag == null)
                    return false;
                bool changed = false;
                var frames = tag.GetFrames<UserTextInformationFrame>()
                    .Where(x => x.Description == "LYRICS")
                    .ToList();
                foreach (var frame in frames)
                {
                    tag.RemoveFrame(frame);
                }

                if (lyrics == null)
                    changed = changed || frames.Count > 0;
                else
                {
                    string lrc = String.Join("\n", lyrics.ToLrc());
                    var frame = new UserTextInformationFrame("LYRICS", StringType.Latin1)
                    {
                        Text = new[] { lrc }
                    };
                    tag.AddFrame(frame);
                    changed = changed || frames.Count != 1 || frames[0].Render(4) != frame.Render(4);
                }

                return changed;
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

                    string? parent = Path.GetDirectoryName(path);
                    if (parent != null)
                        Directory.CreateDirectory(parent);
                    File.WriteAllLines(path, lrc);
                    return true;
                }
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
                    var json = JsonSerializer.Serialize(lyrics);
                    var existing = File.Exists(path) ? File.ReadAllText(path) : null;
                    if (existing != null && json == existing)
                        return false;
                    string? parent = Path.GetDirectoryName(path);
                    if (parent != null)
                        Directory.CreateDirectory(parent);
                    File.WriteAllText(path, json);
                    return true;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private static Lyrics? SyncedFileLyrics(string path, TimeSpan duration)
    {
        path += ".lrc";
        return File.Exists(path) ? LyricsIO.FromLrc(File.ReadLines(path), duration) : null;
    }

    private static Lyrics? RichFileLyrics(string path)
    {
        path += ".lrc.json";
        if (!File.Exists(path))
            return null;
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<Lyrics>(stream);
    }

    public static ChapterCollection? BestChapters(this ExportConfig<ChaptersType> config, TagLib.File file, string path)
    {
        return config.Priority.Select(item => GetChapters(item, file, path)).FirstOrDefault(x => x != null);
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

    public static bool SetChapters(ChapterCollection? chapters, ChaptersType type, TagLib.File file, string path)
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

                    string? parent = Path.GetDirectoryName(path);
                    if (parent != null)
                        Directory.CreateDirectory(parent);
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
                    var json = JsonSerializer.Serialize(chapters);
                    var existing = File.Exists(path) ? File.ReadAllText(path) : null;
                    if (existing != null && json == existing)
                        return false;
                    File.WriteAllText(path, json);
                    return true;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private static ChapterCollection? SimpleFileChapters(string path, TimeSpan duration)
    {
        path += ".chp";
        return File.Exists(path) ? ChaptersIO.FromChp(File.ReadLines(path), duration) : null;
    }

    private static ChapterCollection? RichFileChapters(string path)
    {
        path += ".chp.json";
        if (!File.Exists(path))
            return null;
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<ChapterCollection>(stream);
    }
}

public enum LyricsType
{
    SimpleEmbedded,
    SyncedEmbedded,
    RichEmbedded,
    CustomEmbedded,
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