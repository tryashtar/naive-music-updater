using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class DirectValueSourceFieldSetter : IFieldSetter
    {
        public readonly IValueSource Source;
        public DirectValueSourceFieldSetter(YamlNode yaml)
        {
            Source = ValueSourceFactory.Create(yaml);
        }

        public MetadataProperty Get(IMusicItem item)
        {
            return MetadataProperty.FromValue(Source.Get(item), CombineMode.Replace);
        }

        public MetadataProperty GetWithContext(IMusicItem item, IValue value)
        {
            // discard context
            return Get(item);
        }
    }
}
