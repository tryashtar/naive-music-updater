using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    // represents a type/key of metadata, like "title"
    // handles conversions from string identifiers
    public class MetadataField
    {
        public readonly string Name;
        public readonly MetadataFieldType Type;
        public Predicate<MetadataField> Only => x => x == this;
        public static Predicate<MetadataField> All => x => true;
        private static readonly Dictionary<string, MetadataField> AliasCache = new();
        private MetadataField(string name, MetadataFieldType type, params string[] aliases)
        {
            Name = name;
            Type = type;
            foreach (var value in aliases)
            {
                AliasCache.Add(value, this);
            }
            if (AllFields == null)
                AllFields = new();
            AllFields.Add(this);
        }

        public static MetadataField FromID(string id)
        {
            if (AliasCache.TryGetValue(id, out var result))
                return result;
            return null;
        }

        public static readonly MetadataField Title = new("Title", MetadataFieldType.String, "title");
        public static readonly MetadataField Album = new("Album", MetadataFieldType.String, "album");
        public static readonly MetadataField Performers = new("Performers", MetadataFieldType.StringList, "performer", "performers");
        public static readonly MetadataField AlbumArtists = new("Album Artists", MetadataFieldType.StringList, "album artist", "album artists");
        public static readonly MetadataField Composers = new("Composers", MetadataFieldType.StringList, "composer", "composers");
        public static readonly MetadataField Arranger = new("Arranger", MetadataFieldType.String, "arranger");
        public static readonly MetadataField Comment = new("Comment", MetadataFieldType.String, "comment");
        public static readonly MetadataField Track = new("Track Number", MetadataFieldType.Number, "track");
        public static readonly MetadataField TrackTotal = new("Track Total", MetadataFieldType.Number, "track count", "track total");
        public static readonly MetadataField Year = new("Year", MetadataFieldType.Number, "year");
        public static readonly MetadataField Language = new("Language", MetadataFieldType.String, "lang", "language");
        public static readonly MetadataField Genres = new("Genres", MetadataFieldType.StringList, "genre", "genres");

        private static List<MetadataField> AllFields;
        public static ReadOnlyCollection<MetadataField> Values => AllFields.AsReadOnly();
    }

    public enum MetadataFieldType
    {
        String,
        StringList,
        Number
    }
}
