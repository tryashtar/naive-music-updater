using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public interface IMetadataProperty<T>
    {
        T Value { get; }
        List<T> ListValue { get; }
    }

    public class MetadataProperty<T> : IMetadataProperty<T>
    {
        public T Value { get; private set; }
        public List<T> ListValue => default;
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

    public enum ListCombineMode
    {
        Ignore,
        Replace,
        Append,
        Prepend
    }

    public class MetadataListProperty<T> : IMetadataProperty<T>
    {
        public T Value => default;
        public List<T> ListValue => Values;
        public readonly List<T> Values;
        public readonly ListCombineMode CombineMode;

        private MetadataListProperty(IEnumerable<T> items, ListCombineMode mode)
        {
            Values = items.ToList();
            CombineMode = mode;
        }

        public static MetadataListProperty<T> Create(T item, ListCombineMode mode)
        {
            return new MetadataListProperty<T>(new[] { item }, mode);
        }

        public static MetadataListProperty<T> Create(IEnumerable<T> items, ListCombineMode mode)
        {
            return new MetadataListProperty<T>(items, mode);
        }

        public static MetadataListProperty<T> Delete()
        {
            return new MetadataListProperty<T>(new List<T>(), ListCombineMode.Replace);
        }

        public static MetadataListProperty<T> Ignore()
        {
            return new MetadataListProperty<T>(new List<T>(), ListCombineMode.Ignore);
        }

        public MetadataListProperty<T> CombineWith(MetadataListProperty<T> other)
        {
            if (other.CombineMode == ListCombineMode.Replace)
            {
                Values.Clear();
                Values.AddRange(other.Values);
            }
            if (other.CombineMode == ListCombineMode.Append)
                Values.AddRange(other.Values);
            if (other.CombineMode == ListCombineMode.Prepend)
                Values.InsertRange(0, other.Values);
            return this;
        }

        public MetadataListProperty<U> ConvertTo<U>(Func<T, U> converter)
        {
            return new MetadataListProperty<U>(Values.Select(converter).ToList(), CombineMode);
        }

        public MetadataListProperty<U> TryConvertTo<U>(Func<T, U> converter)
        {
            try
            {
                return ConvertTo(converter);
            }
            catch
            {
                return new MetadataListProperty<U>(default, CombineMode);
            }
        }
    }

    public class Metadata
    {
        public MetadataProperty<string> Title;
        public MetadataProperty<string> Album;
        public MetadataListProperty<string> Performers;
        public MetadataListProperty<string> AlbumArtists;
        public MetadataListProperty<string> Composers;
        public MetadataProperty<string> Arranger;
        public MetadataProperty<string> Comment;
        public MetadataProperty<uint> TrackNumber;
        public MetadataProperty<uint> TrackTotal;
        public MetadataProperty<uint> Year;
        public MetadataProperty<string> Language;
        public MetadataListProperty<string> Genres;

        public Metadata()
        {
            Title = MetadataProperty<string>.Ignore();
            Album = MetadataProperty<string>.Ignore();
            Performers = MetadataListProperty<string>.Ignore();
            AlbumArtists = MetadataListProperty<string>.Ignore();
            Composers = MetadataListProperty<string>.Ignore();
            Arranger = MetadataProperty<string>.Ignore();
            Comment = MetadataProperty<string>.Ignore();
            TrackNumber = MetadataProperty<uint>.Ignore();
            TrackTotal = MetadataProperty<uint>.Ignore();
            Year = MetadataProperty<uint>.Ignore();
            Language = MetadataProperty<string>.Ignore();
            Genres = MetadataListProperty<string>.Ignore();
        }

        public void Merge(Metadata other)
        {
            Title = Title.CombineWith(other.Title);
            Album = Album.CombineWith(other.Album);
            Performers = Performers.CombineWith(other.Performers);
            AlbumArtists = AlbumArtists.CombineWith(other.AlbumArtists);
            Composers = Composers.CombineWith(other.Composers);
            Comment = Comment.CombineWith(other.Comment);
            TrackNumber = TrackNumber.CombineWith(other.TrackNumber);
            TrackTotal = TrackTotal.CombineWith(other.TrackTotal);
            Year = Year.CombineWith(other.Year);
            Language = Language.CombineWith(other.Language);
            Genres = Genres.CombineWith(other.Genres);
        }

        public static Metadata FromMany(IEnumerable<Metadata> many)
        {
            var result = new Metadata();
            foreach (var item in many)
            {
                result.Merge(item);
            }
            return result;
        }
    }
}
