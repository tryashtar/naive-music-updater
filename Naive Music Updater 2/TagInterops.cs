using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using YamlDotNet.RepresentationModel;
using TagLib;
using File = System.IO.File;
using Tag = TagLib.Tag;
using System.Text.RegularExpressions;
using System.Globalization;
using TagLib.Id3v2;
using Microsoft.CSharp.RuntimeBinder;

namespace NaiveMusicUpdater
{
    public interface ITagInterop
    {
        MetadataProperty Get(MetadataField field);
        void Set(MetadataField field, MetadataProperty value);
    }

    public static class TagInteropFactory
    {
        public static ITagInterop GetDynamicInterop(dynamic tag)
        {
            return GetInterop(tag);
        }

        private static ITagInterop GetInterop(TagLib.Id3v2.Tag tag) => new Id3v2TagInterop(tag);
        private static ITagInterop GetInterop(CombinedTag tag) => new MultipleInterop(tag);
    }

    public class MultipleInterop : ITagInterop
    {
        private readonly List<ITagInterop> Interops;
        public MultipleInterop(CombinedTag tag)
        {
            Interops = tag.Tags.Select(x => TagInteropFactory.GetDynamicInterop(x)).ToList();
        }

        public MetadataProperty Get(MetadataField field)
        {
            foreach (var interop in Interops)
            {
                var result = interop.Get(field);
                if (result.Value != null)
                    return result;
            }
            return MetadataProperty.Ignore();
        }

        public void Set(MetadataField field, MetadataProperty value)
        {
            foreach (var interop in Interops)
            {
                interop.Set(field, value);
            }
        }
    }

    public abstract class AbstractIntertop<T> : ITagInterop where T : Tag
    {
        protected delegate MetadataProperty Getter();
        protected delegate void Setter(MetadataProperty value);
        private readonly List<(MetadataField field, Getter getter, Setter setter)> Schema;

        public AbstractIntertop(T tag)
        {
            Schema = CreateSchema(tag);
        }

        protected abstract List<(MetadataField field, Getter getter, Setter setter)> CreateSchema(T tag);

        public MetadataProperty Get(MetadataField field)
        {
            foreach (var entry in Schema)
            {
                if (entry.field == field)
                    return entry.getter();
            }
            throw new InvalidOperationException($"Couldn't get {field.Name} metadata from {typeof(T)}");
        }

        public void Set(MetadataField field, MetadataProperty value)
        {
            foreach (var entry in Schema)
            {
                if (entry.field == field)
                {
                    Replace(entry, value);
                    return;
                }
            }
            throw new InvalidOperationException($"Couldn't get {field.Name} metadata from {typeof(T)}");
        }

        private void Replace((MetadataField field, Getter getter, Setter setter) entry, MetadataProperty incoming)
        {
            var current = entry.getter();
            if (!current.Equals(incoming))
            {
                var old_current = current.ToString();
                current.CombineWith(incoming);
                Logger.WriteLine($"Changing {entry.field.Name} from \"{old_current}\" to \"{current}\"");
                entry.setter(current);
            }
        }

        protected MetadataProperty Get(string str)
        {
            return MetadataProperty.Single(str, CombineMode.Replace);
        }

        protected MetadataProperty Get(uint num)
        {
            return MetadataProperty.Single(num.ToString(), CombineMode.Replace);
        }

        protected MetadataProperty Get(string[] str)
        {
            return MetadataProperty.List(str.ToList(), CombineMode.Replace);
        }

        protected string[] Array(MetadataProperty prop)
        {
            return prop.ListValue.ToArray();
        }

        protected uint Number(MetadataProperty prop)
        {
            if (prop.Value == null)
                return 0;
            return uint.Parse(prop.Value);
        }
    }

    public class Id3v2TagInterop : AbstractIntertop<TagLib.Id3v2.Tag>
    {
        public Id3v2TagInterop(TagLib.Id3v2.Tag tag) : base(tag) { }
        protected override List<(MetadataField field, Getter getter, Setter setter)> CreateSchema(TagLib.Id3v2.Tag tag)
        {
            return new List<(MetadataField field, Getter getter, Setter setter)>
            {
                (MetadataField.Album, () => Get(tag.Album), x => tag.Album = x.Value),
                (MetadataField.AlbumArtists, () => Get(tag.AlbumArtists), x => tag.AlbumArtists = Array(x)),
                (MetadataField.Arranger, () => Get(tag.RemixedBy), x => tag.RemixedBy = x.Value),
                (MetadataField.Comment, () => Get(tag.Comment), x => tag.Comment = x.Value),
                (MetadataField.Composers, () => Get(tag.Composers), x => tag.Composers = Array(x)),
                (MetadataField.Genres, () => Get(tag.Genres), x => tag.Genres = Array(x)),
                (MetadataField.Language, () => Get(GetLanguage(tag)), x => SetLanguage(tag, x.Value)),
                (MetadataField.Performers, () => Get(tag.Performers), x => tag.Performers = Array(x)),
                (MetadataField.Title, () => Get(tag.Title), x => tag.Title = x.Value),
                (MetadataField.Track, () => Get(tag.Track), x => tag.Track = Number(x)),
                (MetadataField.TrackTotal, () => Get(tag.TrackCount), x => tag.TrackCount = Number(x)),
                (MetadataField.Year, () => Get(tag.Year), x => tag.Year = Number(x)),
            };
        }

        private string GetLanguage(TagLib.Id3v2.Tag tag)
        {
            foreach (var frame in tag.GetFrames().ToList())
            {
                if (frame is TextInformationFrame tif && tif.FrameId.ToString() == "TLAN")
                    return tif.Text.Single();
            }
            return null;
        }

        private void SetLanguage(TagLib.Id3v2.Tag tag, string value)
        {
            foreach (var frame in tag.GetFrames().ToList())
            {
                if (frame is TextInformationFrame tif && tif.FrameId.ToString() == "TLAN")
                    tag.RemoveFrame(frame);
            }
            var lang = new TextInformationFrame(ByteVector.FromString("TLAN", StringType.UTF8));
            lang.Text = new[] { value };
            tag.AddFrame(lang);
        }
    }
}
