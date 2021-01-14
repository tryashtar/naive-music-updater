using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class NoOpMetadataStrategy : IMetadataStrategy
    {
        public Metadata Get(IMusicItem item, Predicate<MetadataField> desired)
        {
            return new Metadata();
        }
    }
}
