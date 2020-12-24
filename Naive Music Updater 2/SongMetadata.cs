﻿using System;
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

    public class Metadata
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

        public Metadata()
        {
            Title = MetadataProperty<string>.Ignore();
            Album = MetadataProperty<string>.Ignore();
            Artist = MetadataProperty<string>.Ignore();
            Comment = MetadataProperty<string>.Ignore();
            TrackNumber = MetadataProperty<uint>.Ignore();
            TrackTotal = MetadataProperty<uint>.Ignore();
            Year = MetadataProperty<uint>.Ignore();
            Language = MetadataProperty<string>.Ignore();
            Genre = MetadataProperty<string>.Ignore();
        }

        public void Merge(Metadata other)
        {
            Title = Title.CombineWith(other.Title);
            Album = Album.CombineWith(other.Album);
            Artist = Artist.CombineWith(other.Artist);
            Comment = Comment.CombineWith(other.Comment);
            TrackNumber = TrackNumber.CombineWith(other.TrackNumber);
            TrackTotal = TrackTotal.CombineWith(other.TrackTotal);
            Year = Year.CombineWith(other.Year);
            Language = Language.CombineWith(other.Language);
            Genre = Genre.CombineWith(other.Genre);
        }
    }
}
