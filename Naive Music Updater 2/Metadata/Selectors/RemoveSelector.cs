using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class RemoveSelector : MetadataSelector
    {
        public override MetadataProperty GetRaw(IMusicItem item)
        {
            return MetadataProperty.Delete();
        }
    }
}
