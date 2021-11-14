using System.Collections.Generic;
using System.Linq;
using TagLib;
using Tag = TagLib.Tag;
using TagLib.Id3v2;

namespace NaiveMusicUpdater
{
    public class Id3v2TagInterop : AbstractInterop<TagLib.Id3v2.Tag>
    {
        private static readonly string[] ReadDelimiters = new string[] { "/", "; ", ";" };
        private const string WriteDelimiter = "; ";
        public Id3v2TagInterop(TagLib.Id3v2.Tag tag) : base(tag) { }

        protected override void CustomSetup()
        {
            Tag.ReadArtistDelimiters = ReadDelimiters;
            Tag.WriteArtistDelimiter = WriteDelimiter;
        }

        protected override ByteVector RenderTag()
        {
            return Tag.Render();
        }

        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            var schema = BasicInterop.BasicSchema(Tag);
            schema[MetadataField.Language] = Delegates(() => Get(GetLanguage(Tag)), x => SetLanguage(Tag, Value(x)));
            return schema;
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            var schema = BasicInterop.BasicWipeSchema(Tag);
            schema.Add("compilation", SimpleWipeRet(() => Tag.IsCompilation.ToString(), () => Tag.IsCompilation = false));
            AddFrameWipes(schema);
            return schema;
        }

        private void AddFrameWipes(Dictionary<string, WipeDelegates> schema)
        {

        }

        private const string LANGUAGE_TAG = "TLAN";
        public static string GetLanguage(TagLib.Id3v2.Tag tag)
        {
            foreach (var frame in tag.GetFrames<TextInformationFrame>().ToList())
            {
                if (frame.FrameId.ToString() == LANGUAGE_TAG)
                {
                    if (frame.Text.Length > 0)
                        return frame.Text.First();
                }
            }
            return null;
        }

        public static void SetLanguage(TagLib.Id3v2.Tag tag, string value)
        {
            foreach (var frame in tag.GetFrames<TextInformationFrame>().ToList())
            {
                if (frame.FrameId.ToString() == LANGUAGE_TAG)
                    tag.RemoveFrame(frame);
            }
            var lang = new TextInformationFrame(ByteVector.FromString(LANGUAGE_TAG, StringType.UTF8));
            lang.Text = new[] { value };
            tag.AddFrame(lang);
        }
    }
}
