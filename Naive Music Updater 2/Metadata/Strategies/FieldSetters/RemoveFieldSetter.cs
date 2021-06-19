using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaiveMusicUpdater
{
    public class RemoveFieldSetter : IFieldSetter
    {
        public static readonly RemoveFieldSetter Instance = new();

        public MetadataProperty Get(IMusicItem item)
        {
            return MetadataProperty.Delete();
        }

        public MetadataProperty GetWithContext(IMusicItem item, IValue value)
        {
            // discard context
            return Get(item);
        }
    }
}
