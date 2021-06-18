using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IValueSelector
    {
        IValue Get(IMusicItem item);
    }

    public static class ValueSelectorFactory
    {
        public static IValueSelector Create(YamlNode yaml)
        {
            if (yaml is YamlScalarNode scalar)
            {
                var name = scalar.ToEnum<NameType>();
                if (name == NameType.CleanName)
                    return CleanNameSelector.Instance;
                else if (name == NameType.FileName)
                    return SimpleNameSelector.Instance;
            }
            else if (yaml is YamlMappingNode map)
            {
                var type = map.Go("type").ToEnum<OtherType>();
                if (type == OtherType.Copy)
                {
                    var field = MetadataField.FromID(map.Go("get").String());
                    return new CopyMetadataSelector(field);
                }
            }

            throw new ArgumentException($"Couldn't create a metadata selector from {yaml}");
        }
    }

    public enum NameType
    {
        CleanName,
        FileName
    }

    public enum OtherType
    {
        Copy
    }
}
