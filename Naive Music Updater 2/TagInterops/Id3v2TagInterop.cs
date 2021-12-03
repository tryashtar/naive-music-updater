using System.Collections.Generic;
using System.Linq;
using TagLib;
using Tag = TagLib.Tag;
using TagLib.Id3v2;
using TryashtarUtils.Music;

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
            schema[MetadataField.Language] = Delegates(() => Get(Language.Get(Tag)), x => Language.Set(Tag, Value(x)));
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
    }
}
