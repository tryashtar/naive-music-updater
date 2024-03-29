﻿namespace NaiveMusicUpdater;

public class XiphTagInterop : BacicInterop<TagLib.Ogg.XiphComment>
{
    public XiphTagInterop(TagLib.Ogg.XiphComment tag, LibraryConfig config) : base(tag, config)
    {
    }

    protected override ByteVector RenderTag()
    {
        return Tag.Render(false);
    }

    public override IValue Get(MetadataField field)
    {
        if (field == MetadataField.Year)
        {
            var val = Tag.GetField("YEAR");
            return val.Length == 0 ? BlankValue.Instance : new ListValue(val);
        }

        if (field == MetadataField.Language)
        {
            var val = LanguageExtensions.GetXiph(this.Tag);
            return val == null ? BlankValue.Instance : new StringValue(val);
        }

        if (field == MetadataField.SimpleLyrics)
        {
            var val = Tag.GetFirstField(LyricsIO.OGG_UNSYNCED_LYRICS);
            return val == null ? BlankValue.Instance : new StringValue(val);
        }

        return base.Get(field);
    }

    public override void Set(MetadataField field, IValue value)
    {
        if (field == MetadataField.Art)
            return;
        if (field == MetadataField.Year)
        {
            // TagLib wants to use "DATE" instead of "YEAR", no idea why
            var raw = Get(field);
            var existing = raw.IsBlank ? Array.Empty<string>() : raw.AsList().Values.ToArray();
            var val = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
            if (!existing.SequenceEqual(val))
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                if (value.IsBlank)
                    Tag.RemoveField("YEAR");
                else
                    Tag.SetField("YEAR", val);
            }
        }
        else if (field == MetadataField.Language)
        {
            if (!Config.ShouldKeepXiph(LanguageExtensions.XIPH_LANGUAGE_TAG))
                return;
            var existing = LanguageExtensions.GetXiph(this.Tag) ?? "(blank)";
            var val = value.IsBlank ? null : value.AsString().Value;
            if (LanguageExtensions.SetXiph(this.Tag, val))
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {existing} -> {value}");
        }
        else
        {
            // don't set fields that are going to be cleaned away
            if (field == MetadataField.Title && !Config.ShouldKeepXiph("TITLE"))
                return;
            if (field == MetadataField.Album && !Config.ShouldKeepXiph("ALBUM"))
                return;
            if (field == MetadataField.AlbumArtists && !Config.ShouldKeepXiph("ALBUMARTIST"))
                return;
            if (field == MetadataField.Performers && !Config.ShouldKeepXiph("ARTIST"))
                return;
            if (field == MetadataField.Arranger && !Config.ShouldKeepXiph("REMIXEDBY"))
                return;
            if (field == MetadataField.Composers && !Config.ShouldKeepXiph("COMPOSER"))
                return;
            if (field == MetadataField.Track && !Config.ShouldKeepXiph("TRACKNUMBER"))
                return;
            if (field == MetadataField.TrackTotal && !Config.ShouldKeepXiph("TRACKTOTAL"))
                return;
            if (field == MetadataField.Comment && !Config.ShouldKeepXiph("COMMENT"))
                return;
            if (field == MetadataField.Disc && !Config.ShouldKeepXiph("DISCNUMBER"))
                return;
            if (field == MetadataField.DiscTotal && !Config.ShouldKeepXiph("DISCTOTAL"))
                return;
            base.Set(field, value);
        }
    }

    public override void Clean()
    {
        foreach (var key in Tag.ToList())
        {
            if (!Config.ShouldKeepXiph(key))
            {
                Logger.WriteLine($"{Tag.TagTypes} {key} removed: {new ListValue(Tag.GetField(key))}");
                Tag.RemoveField(key);
            }
        }
    }
}