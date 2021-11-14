using System.Collections.Generic;
using TagLib;
using Tag = TagLib.Tag;

namespace NaiveMusicUpdater
{
    public class ApeTagInterop : AbstractInterop<TagLib.Ape.Tag>
    {
        public ApeTagInterop(TagLib.Ape.Tag tag) : base(tag) { }

        protected override ByteVector RenderTag()
        {
            return Tag.Render();
        }

        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            var schema = BasicInterop.BasicSchema(Tag);
            schema.Remove(MetadataField.Arranger);
            return schema;
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            var schema = BasicInterop.BasicWipeSchema(Tag);
            return schema;
        }
    }
}
