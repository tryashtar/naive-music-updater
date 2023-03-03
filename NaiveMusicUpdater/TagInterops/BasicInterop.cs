namespace NaiveMusicUpdater;

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
        {
            var val = value.IsBlank ? null : value.AsString().Value;
            if (Tag.Album != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Album = val;
            }
        }

        if (field == MetadataField.AlbumArtists)
        {
            var val = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
            if (!Tag.AlbumArtists.SequenceEqual(val))
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.AlbumArtists = val;
            }
        }

        if (field == MetadataField.Arranger)
        {
            var val = value.IsBlank ? null : value.AsString().Value;
            if (Tag.RemixedBy != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.RemixedBy = val;
            }
        }

        if (field == MetadataField.Comment)
        {
            var val = value.IsBlank ? null : value.AsString().Value;
            if (Tag.Comment != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Comment = val;
            }
        }

        if (field == MetadataField.Composers)
        {
            var val = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
            if (!Tag.Composers.SequenceEqual(val))
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Composers = val;
            }
        }

        if (field == MetadataField.Genres)
        {
            var val = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
            if (!Tag.Genres.SequenceEqual(val))
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Genres = val;
            }
        }

        if (field == MetadataField.Performers)
        {
            var val = value.IsBlank ? Array.Empty<string>() : value.AsList().Values.ToArray();
            if (!Tag.Performers.SequenceEqual(val))
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Performers = val;
            }
        }

        if (field == MetadataField.Title)
        {
            var val = value.IsBlank ? null : value.AsString().Value;
            if (Tag.Title != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Title = val;
            }
        }

        if (field == MetadataField.Track)
        {
            var val = value.IsBlank ? 0 : value.AsNumber().Value;
            if (Tag.Track != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Track = val;
            }
        }

        if (field == MetadataField.TrackTotal)
        {
            var val = value.IsBlank ? 0 : value.AsNumber().Value;
            if (Tag.TrackCount != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.TrackCount = val;
            }
        }

        if (field == MetadataField.Disc)
        {
            var val = value.IsBlank ? 0 : value.AsNumber().Value;
            if (Tag.Disc != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Disc = val;
            }
        }

        if (field == MetadataField.DiscTotal)
        {
            var val = value.IsBlank ? 0 : value.AsNumber().Value;
            if (Tag.DiscCount != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.DiscCount = val;
            }
        }

        if (field == MetadataField.Year)
        {
            var val = value.IsBlank ? 0 : value.AsNumber().Value;
            if (Tag.Year != val)
            {
                Logger.WriteLine($"{Tag.TagTypes} {field.DisplayName}: {Get(field)} -> {value}");
                Tag.Year = val;
            }
        }
    }
}