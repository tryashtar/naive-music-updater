using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class ValueMetadataConverter
    {
        public readonly CombineMode Mode;
        public readonly IValueOperator Modify;
        public ValueMetadataConverter(YamlNode yaml)
        {
            Mode = yaml.Go("mode").ToEnum(def: CombineMode.Replace);
            Modify = yaml.Go("modify").Parse(x => ValueOperatorFactory.Create(x));
        }

        public MetadataProperty Convert(IValue value)
        {
            if (Modify != null)
                value = Modify.Apply(value);
            return MetadataProperty.FromValue(value, Mode);
        }
    }
}
