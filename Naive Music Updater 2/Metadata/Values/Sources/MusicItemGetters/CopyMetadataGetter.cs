using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class CopyMetadataGetter : IMusicItemValueSource
    {
        public readonly MetadataField Desired;
        public CopyMetadataGetter(MetadataField desired)
        {
            Desired = desired;
        }

        public IValue Get(IMusicItem item)
        {
            return item.GetMetadata(Desired.Only).Get(Desired);
        }
    }
}
