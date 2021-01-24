using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class SimpleNameSelector : MetadataSelector
    {
        public SimpleNameSelector()
        { }

        public override MetadataProperty GetRaw(IMusicItem item)
        {
            return MetadataProperty.Single(item.SimpleName, CombineMode.Replace);
        }
    }
}
