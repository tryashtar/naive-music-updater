using System.Collections.Generic;
using Tag = TagLib.Tag;

namespace NaiveMusicUpdater
{
    public abstract class BasicInterop : AbstractInterop<Tag>
    {
        public BasicInterop(Tag tag) : base(tag) { }
        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            return BasicSchema(Tag);
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            return BasicWipeSchema(Tag);
        }

        public static Dictionary<MetadataField, InteropDelegates> BasicSchema(Tag tag)
        {
            return new Dictionary<MetadataField, InteropDelegates>
            {
                { MetadataField.Album, Delegates(() => Get(tag.Album), x => tag.Album = Value(x)) },
                { MetadataField.AlbumArtists, Delegates(() => Get(tag.AlbumArtists), x => tag.AlbumArtists = Array(x)) },
                { MetadataField.Arranger, Delegates(() => Get(tag.RemixedBy), x => tag.RemixedBy = Value(x)) },
                { MetadataField.Comment, Delegates(() => Get(tag.Comment), x => tag.Comment = Value(x)) },
                { MetadataField.Composers, Delegates(() => Get(tag.Composers), x => tag.Composers = Array(x)) },
                { MetadataField.Genres, Delegates(() => Get(tag.Genres), x => tag.Genres = Array(x)) },
                { MetadataField.Performers, Delegates(() => Get(tag.Performers), x => tag.Performers = Array(x)) },
                { MetadataField.Title, Delegates(() => Get(tag.Title), x => tag.Title = Value(x)) },
                { MetadataField.Track, NumDelegates(() => Get(tag.Track), x => tag.Track = Number(x)) },
                { MetadataField.TrackTotal, NumDelegates(() => Get(tag.TrackCount), x => tag.TrackCount = Number(x)) },
                { MetadataField.Disc, NumDelegates(() => Get(tag.Disc), x => tag.Disc = Number(x)) },
                { MetadataField.DiscTotal, NumDelegates(() => Get(tag.DiscCount), x => tag.DiscCount = Number(x)) },
                { MetadataField.Year, NumDelegates(() => Get(tag.Year), x => tag.Year = Number(x)) },
            };
        }

        public static Dictionary<string, WipeDelegates> BasicWipeSchema(Tag tag)
        {
            return new Dictionary<string, WipeDelegates>
            {
                { "publisher", SimpleWipe(() => tag.Publisher, () => tag.Publisher = null) },
                { "bpm", SimpleWipe(() => tag.BeatsPerMinute, () => tag.BeatsPerMinute = 0) },
                { "description", SimpleWipe(() => tag.Description, () => tag.Description = null) },
                { "grouping", SimpleWipe(() => tag.Grouping, () => tag.Grouping = null) },
                { "subtitle", SimpleWipe(() => tag.Subtitle, () => tag.Subtitle = null) },
                { "amazon id", SimpleWipe(() => tag.AmazonId, () => tag.AmazonId = null) },
                { "conductor", SimpleWipe(() => tag.Conductor, () => tag.Conductor = null) },
                { "copyright", SimpleWipe(() => tag.Copyright, () => tag.Copyright = null) },
                { "musicbrainz data", SimpleWipe(() => GetMusicBrainz(tag), () => WipeMusicBrainz(tag)) },
                { "music ip", SimpleWipe(() => tag.MusicIpId, () => tag.MusicIpId = null) },
            };
        }

        private static string[] GetMusicBrainz(Tag tag)
        {
            return new string[] {
                tag.MusicBrainzArtistId,
                tag.MusicBrainzDiscId,
                tag.MusicBrainzReleaseArtistId,
                tag.MusicBrainzReleaseCountry,
                tag.MusicBrainzReleaseId,
                tag.MusicBrainzReleaseStatus,
                tag.MusicBrainzReleaseType,
                tag.MusicBrainzTrackId,
            };
        }

        private static void WipeMusicBrainz(Tag tag)
        {
            tag.MusicBrainzArtistId = null;
            tag.MusicBrainzDiscId = null;
            tag.MusicBrainzReleaseArtistId = null;
            tag.MusicBrainzReleaseCountry = null;
            tag.MusicBrainzReleaseId = null;
            tag.MusicBrainzReleaseStatus = null;
            tag.MusicBrainzReleaseType = null;
            tag.MusicBrainzTrackId = null;
        }
    }
}
