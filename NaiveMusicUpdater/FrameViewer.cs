namespace NaiveMusicUpdater;

public static class FrameViewer
{
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