namespace NaiveMusicUpdater;

// represents a type/key of metadata, like "title"
// handles conversions from string identifiers
public class MetadataField
{
    public readonly string DisplayName;
    public Predicate<MetadataField> Only => x => x == this;
    public static Predicate<MetadataField> All => _ => true;
    private static readonly Dictionary<string, MetadataField> AliasCache = new();
    private readonly string[] Aliases;
    public string Id => Aliases[0];

    public MetadataField(string name, params string[] aliases)
    {
        DisplayName = name;
        Aliases = aliases;
        foreach (var value in aliases)
        {
            AliasCache.Add(value, this);
        }

        AllFields.Add(this);
    }

    public override string ToString()
    {
        return DisplayName;
    }

    public static MetadataField FromID(string id)
    {
        if (AliasCache.TryGetValue(id, out var result))
            return result;
        throw new ArgumentException($"No metadata field named {id}");
    }

    public static MetadataField? TryFromID(string id)
    {
        if (AliasCache.TryGetValue(id, out var result))
            return result;
        return null;
    }

    private static readonly List<MetadataField> AllFields = new();
    public static readonly MetadataField Title = new("Title", "title");
    public static readonly MetadataField Album = new("Album", "album");
    public static readonly MetadataField Performers = new("Performers", "performer", "performers");
    public static readonly MetadataField AlbumArtists = new("Album Artists", "album artist", "album artists");
    public static readonly MetadataField Composers = new("Composers", "composer", "composers");
    public static readonly MetadataField Arranger = new("Arranger", "arranger");
    public static readonly MetadataField Comment = new("Comment", "comment");
    public static readonly MetadataField Track = new("Track Number", "track");
    public static readonly MetadataField TrackTotal = new("Track Total", "track count", "track total");
    public static readonly MetadataField Disc = new("Disc Number", "disc");
    public static readonly MetadataField DiscTotal = new("Disc Total", "disc count", "disc total");
    public static readonly MetadataField Year = new("Year", "year");
    public static readonly MetadataField Language = new("Language", "lang", "language");
    public static readonly MetadataField Genres = new("Genres", "genre", "genres");
    public static readonly MetadataField Art = new("Art", "art");
    public static readonly MetadataField SimpleLyrics = new("Simple Lyrics", "simple lyrics", "lyrics");

    public static ReadOnlyCollection<MetadataField> Values => AllFields.AsReadOnly();
}