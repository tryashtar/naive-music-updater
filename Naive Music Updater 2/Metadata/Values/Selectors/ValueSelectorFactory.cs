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
            if (yaml.NodeType == YamlNodeType.Scalar)
            {
                string str = (string)yaml;
                if (str == "clean_name")
                    return CleanNameSelector.Instance;
                if (str == "file_name")
                    return SimpleNameSelector.Instance;
            }
            else if (yaml is YamlMappingNode map)
            {
                var type = (string)map["type"];
                if (type == "copy")
                {
                    var get = (string)map["get"];
                    var field = MetadataField.FromID(get);
                    return new CopyMetadataSelector(field);
                }
            }

            throw new ArgumentException($"Couldn't create a metadata selector from {yaml}");
        }
    }
}
