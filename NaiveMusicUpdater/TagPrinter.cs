namespace NaiveMusicUpdater;

public static class TagPrinter
{
    public static void PrintTags(IEnumerable<string> paths)
    {
        foreach (var item in paths)
        {
            if (File.Exists(item))
            {
                var song = TagLib.File.Create(item);
                Logger.WriteLine(item, ConsoleColor.Green);
                var tag = song.Tag;
                PrintTag(tag);
            }
            else
                Logger.WriteLine($"Not found: {item}", ConsoleColor.Red);

            Logger.WriteLine();
        }
    }

    private static void PrintTag(TagLib.Tag tag)
    {
        var name = tag.GetType().ToString();
        Logger.WriteLine(name, ConsoleColor.Yellow);

        if (tag is TagLib.Id3v2.Tag id3v2)
        {
            foreach (var frame in id3v2.GetFrames().ToList())
            {
                Logger.WriteLine(TagPrinter.ToString(frame));
            }
        }

        if (tag is TagLib.Id3v1.Tag id3v1)
        {
            Logger.WriteLine(id3v1.Render().ToString());
        }

        if (tag is TagLib.Flac.Metadata flac)
        {
            foreach (var pic in flac.Pictures)
            {
                Logger.WriteLine(pic.ToString());
            }
        }

        if (tag is TagLib.Ape.Tag ape)
        {
            foreach (var key in ape)
            {
                var value = ape.GetItem(key);
                Logger.WriteLine($"{key}: {value}");
            }
        }

        if (tag is TagLib.Ogg.XiphComment xiph)
        {
            foreach (var key in xiph)
            {
                var value = xiph.GetField(key);
                Logger.WriteLine($"{key}: {String.Join("\n", value)}");
            }
        }

        if (tag is TagLib.CombinedTag combined)
        {
            foreach (var sub in combined.Tags)
            {
                Logger.TabIn();
                PrintTag(sub);
                Logger.TabOut();
            }
        }
    }
    
    public static string ToString(Frame frame)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Type: {frame.GetType().Name}");
        builder.AppendLine($"ID: {frame.FrameId}");
        builder.AppendLine($"ToString: {frame}");
        if (frame is SynchronisedLyricsFrame lyrics)
        {
            builder.AppendLine($"Synced Lyrics Desc: {lyrics.Description}");
            builder.AppendLine($"Synced Lyrics Format: {lyrics.Format}");
            builder.AppendLine($"Synced Lyrics Language: {lyrics.Language}");
            builder.AppendLine($"Synced Lyrics Encoding: {lyrics.TextEncoding}");
            builder.AppendLine($"Synced Lyrics Type: {lyrics.Type}");
            builder.AppendLine($"Synced Lyrics Text: {LyricsString(lyrics.Text)}");
        }

        if (frame is ChapterFrame chapter)
        {
            builder.AppendLine($"Chapter ID: {chapter.Id}");
            builder.AppendLine($"Chapter Start MS: {chapter.StartMilliseconds}");
            builder.AppendLine($"Chapter End MS: {chapter.EndMilliseconds}");
            builder.AppendLine($"Chapter End Subframes: {String.Join("\n", chapter.SubFrames.Select(ToString))}");
        }

        if (frame is PrivateFrame priv)
        {
            builder.AppendLine($"Private Owner: {priv.Owner}");
            builder.AppendLine($"Private Data: {priv.PrivateData}");
        }

        if (frame is MusicCdIdentifierFrame mcd)
        {
            builder.AppendLine($"MCD Data: {mcd.Data}");
        }

        return builder.ToString();
    }

    private static string LyricsString(SynchedText[] text)
    {
        return String.Join("\n", text.Select(x => $"[{TimeSpan.FromMilliseconds(x.Time)}] {x.Text}"));
    }
}