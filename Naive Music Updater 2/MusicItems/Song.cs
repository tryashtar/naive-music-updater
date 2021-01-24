using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using TagLib.Id3v2;
using TagLib.Matroska;
using File = System.IO.File;

namespace NaiveMusicUpdater
{
    public class Song : IMusicItem
    {
        public string Location { get; private set; }
        protected readonly MusicFolder _Parent;
        public MusicFolder Parent => _Parent;
        public MusicItemConfig LocalConfig => null;
        public LibraryCache GlobalCache => _Parent.GlobalCache;
        public Song(MusicFolder parent, string file)
        {
            _Parent = parent;
            Location = file;
        }

        public void Update()
        {
            Logger.WriteLine($"Song: {SimpleName}");
#if !DEBUG
            if (!GlobalCache.NeedsUpdate(this))
                return;
#endif
            Logger.WriteLine($"(checking)");
            var metadata = MusicItemUtils.GetMetadata(this, MetadataField.All);
            using (TagLib.File file = TagLib.File.Create(Location))
            {
                bool success = true;
                var tag_v1 = (TagLib.Id3v1.Tag)file.GetTag(TagTypes.Id3v1);
                var tag_v2 = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2);
                var path = Util.StringPathAfterRoot(this);
                var art = GlobalCache.GetArtPathFor(this);
                bool changed = false;
                changed |= UpdateTag(tag_v2, metadata);
                changed |= UpdateArt(tag_v2, art);
                changed |= GlobalCache.WriteLyrics(path, tag_v2);
                changed |= WipeUselessProperties(tag_v2);
                changed |= EqualizeTags(tag_v2, tag_v1);
                if (changed)
                {
                    Logger.WriteLine("Saving...");
                    try { file.Save(); }
                    catch (IOException ex)
                    {
                        Logger.WriteLine($"Save failed because {ex.Message}! Skipping...");
                        GlobalCache.MarkNeedsUpdateNextTime(this);
                        success = false;
                    }
                }
                if (success)
                {
#if !DEBUG
                    GlobalCache.Config.NormalizeAudio(this);
                    GlobalCache.MarkUpdatedRecently(this);
#endif
                }
            }
        }

        private bool UpdateArt(TagLib.Tag tag, string art_path)
        {
            var picture = art_path == null ? null : ArtCache.GetPicture(art_path);
            if (!IsSingleValue(tag.Pictures, picture))
            {
                if (picture == null)
                {
                    if (tag.Pictures.Length == 0)
                        return false;
                    Logger.WriteLine($"Deleted art");
                    tag.Pictures = new IPicture[0];
                }
                else
                {
                    Logger.WriteLine($"Added art");
                    tag.Pictures = new IPicture[] { picture };
                }
                return true;
            }
            return false;
        }

        private void ChangedThing(string thing, object old_value, object new_value)
        {
            if (old_value != null)
                Logger.WriteLine($"Deleted {thing}: \"{old_value}\"");
            if (new_value != null)
                Logger.WriteLine($"Added {thing}: \"{new_value}\"");
        }

        private void ChangedThing(string thing, IEnumerable<string> old_value, IEnumerable<string> new_value)
        {
            if (old_value != null)
                Logger.WriteLine($"Deleted {thing}: \"{String.Join(";", old_value)}\"");
            if (new_value != null)
                Logger.WriteLine($"Added {thing}: \"{String.Join(";", new_value)}\"");
        }

        private uint NullableParse(string val)
        {
            if (val == null)
                return 0;
            return uint.Parse(val);
        }

        private bool UpdateTag(TagLib.Id3v2.Tag tag, Metadata metadata)
        {
            bool changed = false;
            string title = metadata.Get(MetadataField.Title).Value;
            if (metadata.Get(MetadataField.Title).CombineMode == CombineMode.Replace && tag.Title != title)
            {
                ChangedThing("title", tag.Title, title);
                tag.Title = title;
                changed = true;
            }
            string album = metadata.Get(MetadataField.Album).Value;
            if (metadata.Get(MetadataField.Album).CombineMode == CombineMode.Replace && tag.Album != album)
            {
                ChangedThing("album", tag.Album, album);
                tag.Album = album;
                changed = true;
            }
            string comment = metadata.Get(MetadataField.Comment).Value;
            if ((tag.Comment == "" || metadata.Get(MetadataField.Comment).CombineMode == CombineMode.Replace) && tag.Comment != comment)
            {
                ChangedThing("comment", tag.Comment, comment);
                tag.Comment = comment;
                changed = true;
            }
            uint track_number = NullableParse(metadata.Get(MetadataField.Track).Value);
            if (metadata.Get(MetadataField.Track).CombineMode == CombineMode.Replace && tag.Track != track_number)
            {
                ChangedThing("track number", tag.Track, track_number);
                tag.Track = track_number;
                changed = true;
            }
            uint track_count = NullableParse(metadata.Get(MetadataField.TrackTotal).Value);
            if (metadata.Get(MetadataField.TrackTotal).CombineMode == CombineMode.Replace && tag.TrackCount != track_count)
            {
                ChangedThing("track count", tag.TrackCount, track_count);
                tag.TrackCount = track_count;
                changed = true;
            }
            uint year = NullableParse(metadata.Get(MetadataField.Year).Value);
            if (metadata.Get(MetadataField.Year).CombineMode == CombineMode.Replace && tag.Year != year)
            {
                ChangedThing("year", tag.Year, year);
                tag.Year = year;
                changed = true;
            }
            string[] genres = metadata.Get(MetadataField.Genres).ListValue.ToArray();
            if (metadata.Get(MetadataField.Genres).CombineMode == CombineMode.Replace && !tag.Genres.SequenceEqual(genres))
            {
                ChangedThing("genre", tag.Genres, genres);
                tag.Genres = genres;
                changed = true;
            }
            string[] album_artists = metadata.Get(MetadataField.AlbumArtists).ListValue.ToArray();
            if (metadata.Get(MetadataField.AlbumArtists).CombineMode == CombineMode.Replace && !tag.AlbumArtists.SequenceEqual(album_artists))
            {
                ChangedThing("album artist", tag.AlbumArtists, album_artists);
                tag.AlbumArtists = album_artists;
                changed = true;
            }
            string[] composers = metadata.Get(MetadataField.Composers).ListValue.ToArray();
            if (metadata.Get(MetadataField.Composers).CombineMode == CombineMode.Replace && !tag.Composers.SequenceEqual(composers))
            {
                ChangedThing("composer", tag.Composers, composers);
                tag.Composers = composers;
                changed = true;
            }
            string[] performers = metadata.Get(MetadataField.Performers).ListValue.ToArray();
            if (metadata.Get(MetadataField.Performers).CombineMode == CombineMode.Replace && !tag.Performers.SequenceEqual(performers))
            {
                ChangedThing("performer", tag.Performers, performers);
                tag.Performers = performers;
                changed = true;
            }
            string arranger = metadata.Get(MetadataField.Arranger).Value;
            if (metadata.Get(MetadataField.Arranger).CombineMode == CombineMode.Replace && tag.RemixedBy != arranger)
            {
                ChangedThing("arranger", tag.RemixedBy, arranger);
                tag.RemixedBy = arranger;
                changed = true;
            }
            string language = metadata.Get(MetadataField.Language).Value;
            if (metadata.Get(MetadataField.Language).CombineMode == CombineMode.Replace)
            {
                bool found_language = language == null;
                foreach (var frame in tag.GetFrames().ToList())
                {
                    if (frame is TextInformationFrame tif && tif.FrameId.ToString() == "TLAN")
                    {
                        string current = tif.Text.Single();
                        if (!found_language)
                        {
                            found_language = true;
                            if (tif.Text.Single() != language)
                            {
                                ChangedThing("language frame", current, language);
                                tif.Text = new[] { language };
                                changed = true;
                            }
                        }
                        else
                        {
                            Logger.WriteLine($"Removing extra language frame: {current}");
                            tag.RemoveFrame(frame);
                            changed = true;
                        }
                    }
                }
                if (!found_language)
                {
                    Logger.WriteLine($"Creating new language frame: {language}");
                    var frame = new TextInformationFrame(ByteVector.FromString("TLAN", StringType.UTF8));
                    frame.Text = new[] { language };
                    tag.AddFrame(frame);
                    changed = true;
                }
            }
            return changed;
        }

        private string Resize(string thing, int size)
        {
            if (thing == null)
                return null;
            return TagLib.Id3v1.Tag.DefaultStringHandler.Render(thing).Resize(size).ToString().Trim().TrimEnd('\0');
        }

        private bool EqualizeTags(TagLib.Id3v2.Tag v2, TagLib.Id3v1.Tag v1)
        {
            bool changed = false;
            if (Resize(v2.Title, 30) != Resize(v1.Title, 30))
            {
                Logger.WriteLine($"Updated title in V1 tag ({v1.Title}) to match V2 ({v2.Title})");
                v1.Title = v2.Title;
                changed = true;
            }
            if (!v2.Performers.Select(x => Resize(x, 30)).SequenceEqual(v1.Performers.Select(x => Resize(x, 30))))
            {
                Logger.WriteLine($"Updated performer in V1 tag ({String.Join(";", v1.Performers)}) to match V2 ({String.Join(";", v2.Performers)})");
                v1.Performers = v2.Performers;
                changed = true;
            }
            if (Resize(v2.RemixedBy, 30) != Resize(v1.RemixedBy, 30))
            {
                Logger.WriteLine($"Updated arranger in V1 tag ({v1.RemixedBy}) to match V2 ({v2.RemixedBy})");
                v1.RemixedBy = v2.RemixedBy;
                changed = true;
            }
            if (Resize(v2.Album, 30) != Resize(v1.Album, 30))
            {
                Logger.WriteLine($"Updated album in V1 tag ({v1.Album}) to match V2 ({v2.Album})");
                v1.Album = v2.Album;
                changed = true;
            }
            if (Resize(v2.Comment, 28) != Resize(v1.Comment, 28))
            {
                Logger.WriteLine($"Updated comment in V1 tag ({v1.Comment}) to match V2 ({v2.Comment})");
                v1.Comment = v2.Comment;
                changed = true;
            }
            if (v2.Year != v1.Year)
            {
                Logger.WriteLine($"Updated year in V1 tag ({v1.Year}) to match V2 ({v2.Year})");
                v1.Year = v2.Year;
                changed = true;
            }
            uint track = v2.Track;
            if (track > 255)
                track = 0;
            if (track != v1.Track)
            {
                Logger.WriteLine($"Updated track in V1 tag ({v1.Track}) to match V2 ({track})");
                if (track != v2.Track)
                    Logger.WriteLine($"(Couldn't store real value of {v2.Track})");
                v1.Track = track;
                changed = true;
            }
            // don't copy track count, it doesn't actually save
            if (!v2.Genres.SequenceEqual(v1.Genres))
            {
                Logger.WriteLine($"Updated genre in V1 tag ({String.Join(";", v1.Genres)}) to match V2 ({String.Join(";", v2.Genres)})");
                v1.Genres = v2.Genres;
                changed = true;
            }
            return changed;
        }

        // returns whether this changed anything
        private bool WipeUselessProperties(TagLib.Tag tag)
        {
            bool changed = false;
            if (tag is TagLib.Id3v2.Tag tag_v2)
            {
                if (tag_v2.IsCompilation != false)
                {
                    ChangedThing("compilation", tag_v2.IsCompilation, null);
                    tag_v2.IsCompilation = false;
                    changed = true;
                }
                foreach (var frame in tag_v2.GetFrames().ToList())
                {
                    bool remove = false;
                    if (frame is TextInformationFrame tif)
                    {
                        if (tif.Text.Length == 0)
                        {
                            Logger.WriteLine($"Removed text information frame with length {tif.Text.Length}: \"{tif}\"");
                            remove = true;
                        }
                        else
                        {
                            var tiftext = tif.Text.First();
                            var id = tif.FrameId.ToString();
                            if (!new string[] {
                                "TIT2", // title
                                "TALB", // album
                                "TPE1", // artist
                                "TPE2", // performer
                                "TCOM", // composer
                                "TRCK", // track number
                                "TLAN", // language
                                "TDRC", // year
                                "TCON", // genre
                            }.Contains(id) && !tiftext.StartsWith("[replaygain_"))
                            {
                                Logger.WriteLine($"Removed text information frame of type {id} not carrying tag data: \"{tif}\"");
                                remove = true;
                            }
                        }
                    }
                    else if (frame is CommentsFrame cf)
                    {
                        if (cf.Text != tag.Comment)
                        {
                            Logger.WriteLine($"Removed comment frame not matching comment: \"{cf}\"");
                            remove = true;
                        }
                    }
                    else if (frame is UnsynchronisedLyricsFrame ulf)
                    {
                        if (ulf.Text != tag.Lyrics)
                        {
                            Logger.WriteLine($"Removed lyrics frame not matching lyrics: \"{ulf}\"");
                            remove = true;
                        }
                    }
                    else if (frame is MusicCdIdentifierFrame mcd)
                    {
                        Logger.WriteLine($"Removed music CD identifier frame: \"{mcd.Data}\"");
                        remove = true;
                    }
                    else if (frame is UrlLinkFrame urlf)
                    {
                        Logger.WriteLine($"Removed URL link frame: \"{urlf}\"");
                        remove = true;
                    }
                    else if (frame is UnknownFrame unkf)
                    {
                        Logger.WriteLine($"Removed unknown frame: \"{unkf.Data}\"");
                        remove = true;
                    }
                    else if (frame is PrivateFrame pf)
                    {
                        if (GlobalCache.Config.IsIllegalPrivateOwner(pf.Owner))
                        {
                            Logger.WriteLine($"Removed private frame with owner \"{pf.Owner}\": \"{pf.PrivateData}\"");
                            remove = true;
                        }
                    }
                    if (remove)
                    {
                        tag_v2.RemoveFrame(frame);
                        changed = true;
                    }
                }
            }
            if (tag.Publisher != null)
            {
                ChangedThing("publisher", tag.Publisher, null);
                tag.Publisher = null;
                changed = true;
            }
            if (tag.BeatsPerMinute != 0)
            {
                ChangedThing("beats per minute", tag.BeatsPerMinute, null);
                tag.BeatsPerMinute = 0;
                changed = true;
            }
            if (tag.Description != null)
            {
                ChangedThing("description", tag.Description, null);
                tag.Description = null;
                changed = true;
            }
            if (tag.Grouping != null)
            {
                ChangedThing("grouping", tag.Grouping, null);
                tag.Grouping = null;
                changed = true;
            }
            if (tag.Subtitle != null)
            {
                ChangedThing("subtitle", tag.Subtitle, null);
                tag.Subtitle = null;
                changed = true;
            }
            if (tag.AmazonId != null)
            {
                ChangedThing("amazon id", tag.AmazonId, null);
                tag.AmazonId = null;
                changed = true;
            }
            if (tag.Conductor != null)
            {
                ChangedThing("conductor", tag.Conductor, null);
                tag.Conductor = null;
                changed = true;
            }
            if (tag.Copyright != null)
            {
                ChangedThing("copyright", tag.Copyright, null);
                tag.Copyright = null;
                changed = true;
            }
            if (tag.Disc != 0)
            {
                ChangedThing("disc number", tag.Disc, null);
                tag.Disc = 0;
                changed = true;
            }
            if (tag.DiscCount != 0)
            {
                ChangedThing("disc count", tag.DiscCount, null);
                tag.DiscCount = 0;
                changed = true;
            }
            if (tag.MusicBrainzArtistId != null ||
                tag.MusicBrainzDiscId != null ||
                tag.MusicBrainzReleaseArtistId != null ||
                tag.MusicBrainzReleaseCountry != null ||
                tag.MusicBrainzReleaseId != null ||
                tag.MusicBrainzReleaseStatus != null ||
                tag.MusicBrainzReleaseType != null ||
                tag.MusicBrainzTrackId != null)
            {
                ChangedThing("musicbrainz data", new string[] { tag.MusicBrainzArtistId,
                tag.MusicBrainzDiscId,
                tag.MusicBrainzReleaseArtistId,
                tag.MusicBrainzReleaseCountry,
                tag.MusicBrainzReleaseId,
                tag.MusicBrainzReleaseStatus,
                tag.MusicBrainzReleaseType,
                tag.MusicBrainzTrackId, }, null);

                tag.MusicBrainzArtistId = null;
                tag.MusicBrainzDiscId = null;
                tag.MusicBrainzReleaseArtistId = null;
                tag.MusicBrainzReleaseCountry = null;
                tag.MusicBrainzReleaseId = null;
                tag.MusicBrainzReleaseStatus = null;
                tag.MusicBrainzReleaseType = null;
                tag.MusicBrainzTrackId = null;
                changed = true;
            }
            if (tag.MusicIpId != null)
            {
                ChangedThing("music IP", tag.MusicIpId, null);
                tag.MusicIpId = null;
                changed = true;
            }
            return changed;
        }

        private static bool IsSingleValue(IPicture[] array, IPicture value)
        {
            if (array == null)
                return false;
            if (value == null)
                return array.Length == 0;
            if (array.Length != 1)
                return false;

            return value.Data == array[0].Data;
        }

        public string SimpleName => Path.GetFileNameWithoutExtension(this.Location);

        public IEnumerable<IMusicItem> PathFromRoot() => MusicItemUtils.PathFromRoot(this);
        public MusicLibrary RootLibrary => (MusicLibrary)PathFromRoot().First();
    }
}
