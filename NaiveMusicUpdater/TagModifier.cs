using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NaiveMusicUpdater;

public class TagModifier
{
    public bool HasChanged { get; private set; }
    private readonly TagLib.File TagFile;
    private readonly LibraryCache Cache;
    public TagModifier(TagLib.File file, LibraryCache cache)
    {
        TagFile = file;
        Cache = cache;
    }

    public void UpdateMetadata(Metadata metadata)
    {
        var interop = TagInteropFactory.GetDynamicInterop(TagFile.Tag, Cache.Config);
        if (interop.Changed)
            Logger.WriteLine("Change detected from creating interop!", ConsoleColor.Red);
        foreach (var field in MetadataField.Values)
        {
            interop.Set(field, metadata.Get(field));
        }
        interop.WipeUselessProperties();
        if (interop.Changed)
            HasChanged = true;
    }

    private static Lyrics? Better(Lyrics? l1, Lyrics? l2)
    {
        if (l1 == null)
            return l2;
        if (l2 == null)
            return l1;
        if (!l1.AllLyrics.Any())
            return l2;
        if (!l2.AllLyrics.Any())
            return l1;
        return l1;
    }

    private static ChapterCollection? Better(ChapterCollection? l1, ChapterCollection? l2)
    {
        if (l1 == null)
            return l2;
        if (l2 == null)
            return l1;
        if (l1.Chapters.Count == 0)
            return l2;
        if (l2.Chapters.Count == 0)
            return l1;
        return l1;
    }

    public void WriteLyrics(string location)
    {
        var lyrics_file = Path.Combine(Cache.Folder, "lyrics", location) + ".lrc";
        var rich_file = Path.Combine(Cache.Folder, "lyrics", location) + ".lrc.json";
        var cached_text = File.Exists(lyrics_file) ? File.ReadAllLines(lyrics_file) : null;
        var rich_text = File.Exists(rich_file) ? File.ReadAllText(rich_file) : null;
        var rich_json = rich_text == null ? null : JObject.Parse(rich_text);

        var embedded = LyricsIO.FromFile(TagFile);
        var cached = cached_text == null ? null : LyricsIO.FromLrc(cached_text);
        var rich = rich_text == null ? null : LyricsIO.FromJson(rich_json);
        var best = Better(embedded, Better(rich, cached));
        if (best != null && !best.AllLyrics.Any())
            best = null; // wipe when empty

        if (LyricsIO.ToFile(TagFile, best, LyricTypes.Simple | LyricTypes.Synced | LyricTypes.Rich))
        {
            Logger.WriteLine($"Rewriting embedded lyrics");
            HasChanged = true;
        }

        if (best != cached && best != null)
        {
            var writing = best.ToLrc();
            if (cached_text == null || !cached_text.SequenceEqual(writing))
            {
                Logger.WriteLine($"Rewriting cached lyrics");
                Directory.CreateDirectory(Path.GetDirectoryName(lyrics_file)!);
                File.WriteAllLines(lyrics_file, writing);
            }
        }

        if (best != rich && best != null)
        {
            var writing = LyricsIO.ToJson(best).ToString(Formatting.Indented);
            if (rich_text == null || rich_text != writing)
            {
                Logger.WriteLine($"Rewriting cached lyrics JSON");
                Directory.CreateDirectory(Path.GetDirectoryName(rich_file)!);
                File.WriteAllText(rich_file, writing);
            }
        }
    }

    public void WriteChapters(string location)
    {
        var chapters_file = Path.Combine(Cache.Folder, "chapters", location) + ".chp";
        var rich_file = Path.Combine(Cache.Folder, "chapters", location) + ".chp.json";
        var cached_text = File.Exists(chapters_file) ? File.ReadAllLines(chapters_file) : null;
        var rich_text = File.Exists(rich_file) ? File.ReadAllText(rich_file) : null;
        var rich_json = rich_text == null ? null : JObject.Parse(rich_text);

        var embedded = ChaptersIO.FromFile(TagFile);
        var cached = cached_text == null ? null : ChaptersIO.FromChp(cached_text);
        var rich = rich_text == null ? null : ChaptersIO.FromJson(rich_json);
        var best = Better(embedded, Better(rich, cached));
        if (best != null && best.Chapters.Count == 0)
            best = null; // wipe when empty

        if (ChaptersIO.ToFile(TagFile, best))
        {
            Logger.WriteLine($"Rewriting embedded chapters");
            HasChanged = true;
        }

        if (best != cached && best != null)
        {
            var writing = best.ToChp();
            if (cached_text == null || !cached_text.SequenceEqual(writing))
            {
                Logger.WriteLine($"Rewriting cached chapters");
                Directory.CreateDirectory(Path.GetDirectoryName(chapters_file)!);
                File.WriteAllLines(chapters_file, writing);
            }
        }

        if (best != rich && best != null)
        {
            var writing = ChaptersIO.ToJson(best).ToString(Formatting.Indented);
            if (rich_text == null || rich_text != writing)
            {
                Logger.WriteLine($"Rewriting cached lyrics JSON");
                Directory.CreateDirectory(Path.GetDirectoryName(rich_file)!);
                File.WriteAllText(rich_file, writing);
            }
        }
    }

    public void UpdateArt(string? art_path)
    {
        var picture = art_path == null ? null : ArtCache.GetPicture(art_path);
        if (!IsSingleValue(TagFile.Tag.Pictures, picture))
        {
            if (picture == null)
            {
                if (TagFile.Tag.Pictures.Length == 0)
                    return;
                Logger.WriteLine($"Deleted art");
                TagFile.Tag.Pictures = new IPicture[0];
            }
            else
            {
                Logger.WriteLine($"Added art");
                TagFile.Tag.Pictures = new IPicture[] { picture };
            }
            HasChanged = true;
        }
    }

    private static bool IsSingleValue(IPicture[]? array, IPicture? value)
    {
        if (array == null)
            return false;
        if (value == null)
            return array.Length == 0;
        if (array.Length != 1)
            return false;

        return value.Data == array[0].Data;
    }
}
