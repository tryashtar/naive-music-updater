using System;
using System.Collections.Generic;
using TagLib;
using Tag = TagLib.Tag;

namespace NaiveMusicUpdater
{
    public class XiphTagInterop : AbstractInterop<TagLib.Ogg.XiphComment>
    {
        public XiphTagInterop(TagLib.Ogg.XiphComment tag, LibraryConfig config) : base(tag, config) { }

        protected override ByteVector RenderTag()
        {
            return Tag.Render(false);
        }

        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            var schema = BasicInterop.BasicSchema(Tag);
            schema[MetadataField.Year] = new InteropDelegates(() => Get(Tag.GetField("YEAR")), x => Tag.SetField("YEAR", Number(x)), NumberEqual);
            return schema;
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            var schema = BasicInterop.BasicWipeSchema(Tag);
            AddFieldWipes(schema, "LABEL", "ISRC", "BARCODE");
            return schema;
        }

        private void AddFieldWipes(Dictionary<string, WipeDelegates> schema, params string[] fields)
        {
            foreach (var field in fields)
            {
                schema.Add(field, SimpleWipe(() => String.Join(";", Tag.GetField(field)), () => Tag.RemoveField(field)));
            }
        }
    }
}
