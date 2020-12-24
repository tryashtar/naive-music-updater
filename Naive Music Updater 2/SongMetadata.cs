using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class MetadataProperty<T>
    {
        public readonly T Value;
        public readonly bool Overwrite;

        private MetadataProperty(T item, bool overwrite)
        {
            Value = item;
            Overwrite = overwrite;
        }

        public static MetadataProperty<T> Create(T item)
        {
            return new MetadataProperty<T>(item, true);
        }

        public static MetadataProperty<T> Delete()
        {
            return new MetadataProperty<T>(default, true);
        }

        public static MetadataProperty<T> Ignore()
        {
            return new MetadataProperty<T>(default, false);
        }

        public MetadataProperty<T> CombineWith(MetadataProperty<T> other)
        {
            if (other.Overwrite)
                return other;
            return this;
        }

        public MetadataProperty<U> ConvertTo<U>(Func<T, U> converter)
        {
            return new MetadataProperty<U>(converter(Value), Overwrite);
        }

        public MetadataProperty<U> TryConvertTo<U>(Func<T, U> converter)
        {
            try
            {
                return ConvertTo(converter);
            }
            catch
            {
                return new MetadataProperty<U>(default, Overwrite);
            }
        }
    }

    public class SongMetadataBuilder
    {
        public MetadataProperty<string> Title;
        public MetadataProperty<string> Album;
        public MetadataProperty<string> Artist;
        public MetadataProperty<string> Comment;
        public MetadataProperty<uint> TrackNumber;
        public MetadataProperty<uint> TrackTotal;
        public MetadataProperty<uint> Year;
        public MetadataProperty<string> Language;
        public MetadataProperty<string> Genre;
        public SongMetadataBuilder()
        { }

        public SongMetadata Build()
        {
            return new SongMetadata(
                Title ?? MetadataProperty<string>.Ignore(),
                Album ?? MetadataProperty<string>.Ignore(),
                Artist ?? MetadataProperty<string>.Ignore(),
                Comment ?? MetadataProperty<string>.Ignore(),
                TrackNumber ?? MetadataProperty<uint>.Ignore(),
                TrackTotal ?? MetadataProperty<uint>.Ignore(),
                Year ?? MetadataProperty<uint>.Ignore(),
                Language ?? MetadataProperty<string>.Ignore(),
                Genre ?? MetadataProperty<string>.Ignore()
            );
        }
    }

    public class SongMetadata
    {
        public readonly MetadataProperty<string> Title;
        public readonly MetadataProperty<string> Album;
        public readonly MetadataProperty<string> Artist;
        public readonly MetadataProperty<string> Comment;
        public readonly MetadataProperty<uint> TrackNumber;
        public readonly MetadataProperty<uint> TrackTotal;
        public readonly MetadataProperty<uint> Year;
        public readonly MetadataProperty<string> Language;
        public readonly MetadataProperty<string> Genre;

        public SongMetadata(
           // these aren't allowed to be null
           MetadataProperty<string> title,
           MetadataProperty<string> album,
           MetadataProperty<string> artist,
           MetadataProperty<string> comment,
           MetadataProperty<uint> track_number,
           MetadataProperty<uint> track_total,
           MetadataProperty<uint> year,
           MetadataProperty<string> language,
           MetadataProperty<string> genre
        )
        {
            Title = title;
            Album = album;
            Artist = artist;
            Comment = comment;
            TrackNumber = track_number;
            TrackTotal = track_total;
            Year = year;
            Language = language;
            Genre = genre;
        }

        public SongMetadata Combine(SongMetadata other)
        {
            return new SongMetadata(
                Title.CombineWith(other.Title),
                Album.CombineWith(other.Album),
                Artist.CombineWith(other.Artist),
                Comment.CombineWith(other.Comment),
                TrackNumber.CombineWith(other.TrackNumber),
                TrackTotal.CombineWith(other.TrackTotal),
                Year.CombineWith(other.Year),
                Language.CombineWith(other.Language),
                Genre.CombineWith(other.Genre)
            );
        }

        public static SongMetadata Merge(IEnumerable<SongMetadata> metas)
        {
            if (!metas.Any())
                return new SongMetadataBuilder().Build();
            return metas.Aggregate((x, y) => x.Combine(y));
        }
    }
}
