using System.Collections.Generic;
using System.Linq;
using TagLib;
using Tag = TagLib.Tag;

namespace NaiveMusicUpdater
{
    public class AppleTagInterop : AbstractInterop<TagLib.Mpeg4.AppleTag>
    {
        public AppleTagInterop(TagLib.Mpeg4.AppleTag tag) : base(tag) { }

        protected override ByteVector RenderTag()
        {
            var vector = new ByteVector();
            foreach (var data in Tag.Select(x => x.Render()))
            {
                vector.Add(data);
            }
            return vector;
        }

        protected override Dictionary<MetadataField, InteropDelegates> CreateSchema()
        {
            var schema = BasicInterop.BasicSchema(Tag);
            return schema;
        }

        protected override Dictionary<string, WipeDelegates> CreateWipeSchema()
        {
            var schema = BasicInterop.BasicWipeSchema(Tag);
            return schema;
        }
    }
}
