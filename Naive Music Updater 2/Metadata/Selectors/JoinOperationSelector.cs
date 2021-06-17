using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class JoinOperationSelector : MetadataSelector
    {
        private readonly MetadataSelector From1;
        private readonly MetadataSelector From2;
        private readonly string With;

        // gets metadata "From" two other places and combines them with "With" in between
        public JoinOperationSelector(YamlMappingNode yaml)
        {
            From1 = MetadataSelectorFactory.Create(yaml["from1"]);
            From2 = MetadataSelectorFactory.Create(yaml["from2"]);
            With = (string)yaml["with"];
        }

        public override MetadataProperty GetRaw(IMusicItem item)
        {
            var text1 = From1.GetRaw(item);
            var text2 = From2.GetRaw(item);
            if (text1 == null && text2 == null)
                return null;
            if (text1 == null)
                return text2;
            if (text2 == null)
                return text1;
            return MetadataProperty.Single(text1.Value + With + text2.Value, CombineMode.Replace);
        }
    }
}
