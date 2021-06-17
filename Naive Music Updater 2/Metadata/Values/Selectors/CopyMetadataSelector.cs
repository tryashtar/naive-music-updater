using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class CopyMetadataSelector : IValueSelector
    {
        public readonly MetadataField Desired;
        public CopyMetadataSelector(MetadataField desired)
        {
            Desired = desired;
        }

        public IValue Get(IMusicItem item)
        {
            return item.GetMetadata(Desired.Only).Get(Desired);
        }
    }
}
