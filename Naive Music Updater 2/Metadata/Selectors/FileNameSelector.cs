using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class FilenameSelector : MetadataSelector
    {
        public FilenameSelector()
        { }

        public override string GetRaw(IMusicItem item)
        {
            return ResolveNameOrDefault(item, item);
        }
    }
}
