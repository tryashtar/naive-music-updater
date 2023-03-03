﻿namespace NaiveMusicUpdater;

public abstract class BacicInterop<T> : AbstractInterop<T> where T : Tag
{
    public BacicInterop(T tag, LibraryConfig config) : base(tag, config)
    {
    }

    public override IValue Get(MetadataField field)
    {
        if (field == MetadataField.Album)
            return Tag.Album == null ? BlankValue.Instance : new StringValue(Tag.Album);
        if (field == MetadataField.AlbumArtists)
            return Tag.AlbumArtists.Length == 0 ? BlankValue.Instance : new ListValue(Tag.AlbumArtists);
        if (field == MetadataField.Arranger)
            return Tag.RemixedBy == null ? BlankValue.Instance : new StringValue(Tag.RemixedBy);
        if (field == MetadataField.Comment)
            return Tag.Comment == null ? BlankValue.Instance : new StringValue(Tag.Comment);
        if (field == MetadataField.Composers)
            return Tag.Composers.Length == 0 ? BlankValue.Instance : new ListValue(Tag.Composers);
        if (field == MetadataField.Genres)
            return Tag.Genres.Length == 0 ? BlankValue.Instance : new ListValue(Tag.Genres);
        if (field == MetadataField.Performers)
            return Tag.Performers.Length == 0 ? BlankValue.Instance : new ListValue(Tag.Performers);
        if (field == MetadataField.Title)
            return Tag.Title == null ? BlankValue.Instance : new StringValue(Tag.Title);
        if (field == MetadataField.Track)
            return Tag.Track == 0 ? BlankValue.Instance : new NumberValue(Tag.Track);
        if (field == MetadataField.TrackTotal)
            return Tag.TrackCount == 0 ? BlankValue.Instance : new NumberValue(Tag.TrackCount);
        if (field == MetadataField.Disc)
            return Tag.Disc == 0 ? BlankValue.Instance : new NumberValue(Tag.Disc);
        if (field == MetadataField.DiscTotal)
            return Tag.DiscCount == 0 ? BlankValue.Instance : new NumberValue(Tag.DiscCount);
        if (field == MetadataField.Year)
            return Tag.Year == 0 ? BlankValue.Instance : new NumberValue(Tag.Year);
        throw new ArgumentException(nameof(field));
    }

    public override void Set(MetadataField field, IValue value)
    {
        if (field == MetadataField.Album)
            Tag.Album = value.IsBlank ? null : value.AsString().Value;
        if (field == MetadataField.AlbumArtists)
            Tag.AlbumArtists = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
        if (field == MetadataField.Arranger)
            Tag.RemixedBy = value.IsBlank ? null : value.AsString().Value;
        if (field == MetadataField.Comment)
            Tag.Comment = value.IsBlank ? null : value.AsString().Value;
        if (field == MetadataField.Composers)
            Tag.Composers = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
        if (field == MetadataField.Genres)
            Tag.Genres = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
        if (field == MetadataField.Performers)
            Tag.Performers = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
        if (field == MetadataField.Title)
            Tag.Title = value.IsBlank ? null : value.AsString().Value;
        if (field == MetadataField.Track)
            Tag.Track = value.IsBlank ? 0 : value.AsNumber().Value;
        if (field == MetadataField.TrackTotal)
            Tag.TrackCount = value.IsBlank ? 0 : value.AsNumber().Value;
        if (field == MetadataField.Disc)
            Tag.Disc = value.IsBlank ? 0 : value.AsNumber().Value;
        if (field == MetadataField.DiscTotal)
            Tag.DiscCount = value.IsBlank ? 0 : value.AsNumber().Value;
        if (field == MetadataField.Year)
            Tag.Year = value.IsBlank ? 0 : value.AsNumber().Value;
    }
}