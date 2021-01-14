using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class CopyMetadataSelector : MetadataSelector
    {
        public delegate MetadataProperty MetadataGetter(Metadata meta);
        private readonly MetadataGetter Getter;
        private readonly Predicate<MetadataField> Desired;
        public CopyMetadataSelector(MetadataGetter getter)
        {
            Getter = getter;
        }

        public CopyMetadataSelector(YamlMappingNode yaml)
        {
            var get = (string)yaml["get"];
            var field = MetadataField.FromID(get);
            Getter = x => x.Get(field);
            Desired = field.Only;
        }

        public override string GetRaw(IMusicItem item)
        {
            return Getter(item.GetMetadata(Desired)).Value;
        }

        public override string[] GetRawList(IMusicItem item)
        {
            return Getter(item.GetMetadata(Desired)).ListValue.ToArray();
        }
    }
}
