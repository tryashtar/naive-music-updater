using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public class ModeContextFieldSetter : IFieldSetter
    {
        public readonly CombineMode Mode;
        public readonly IValueOperator Modify;
        public ModeContextFieldSetter(YamlNode yaml)
        {
            Mode = yaml.Go("mode").ToEnum(def: CombineMode.Replace);
            Modify = yaml.Go("modify").Parse(x => ValueOperatorFactory.Create(x));
        }

        public MetadataProperty Get(IMusicItem item)
        {
            throw new InvalidOperationException($"Performing an operation on a value requires context!");
        }

        public MetadataProperty GetWithContext(IMusicItem item, IValue value)
        {
            value = Modify.Apply(value);
            return MetadataProperty.FromValue(value, Mode);
        }
    }
}
