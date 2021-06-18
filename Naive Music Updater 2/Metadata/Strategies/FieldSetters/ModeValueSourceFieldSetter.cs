using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class ModeValueSourceFieldSetter : IFieldSetter
    {
        public readonly CombineMode Mode;
        public readonly IValueSource Source;
        public ModeValueSourceFieldSetter(YamlNode yaml)
        {
            Mode = yaml.Go("mode").ToEnum(def: CombineMode.Replace);
            Source = yaml.Go("source").NullableParse(x => ValueSourceFactory.Create(x));
        }

        public MetadataProperty Get(IMusicItem item)
        {
            if (Mode == CombineMode.Remove)
                return MetadataProperty.Delete();
            else
                return MetadataProperty.FromValue(Source.Get(item), Mode);
        }

        public MetadataProperty GetWithContext(IMusicItem item, IValue value)
        {
            // discard context
            return Get(item);
        }
    }
}
