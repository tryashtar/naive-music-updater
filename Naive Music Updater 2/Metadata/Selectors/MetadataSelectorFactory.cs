using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public static class MetadataSelectorFactory
    {
        public static MetadataSelector FromToken(YamlNode yaml)
        {
            if (yaml.NodeType == YamlNodeType.Scalar)
            {
                string val = (string)yaml;
                if (val == "<this>")
                    return new FilenameSelector();
                return new LiteralSelector(val);
            }
            if (yaml is YamlMappingNode map)
            {
                var operation = (string)map.TryGet("operation");
                if (operation != null)
                {
                    if (operation == "split")
                        return new SplitOperationSelector(map);
                    else if (operation == "join")
                        return new JoinOperationSelector(map);
                    else if (operation == "regex")
                        return new RegexSelector(map);
                    else if (operation == "copy")
                        return new CopyMetadataSelector(map);
                    else if (operation == "parent")
                    {
                        var up = map.TryGet("up");
                        return new SimpleParentSelector(int.Parse((string)up));
                    }
                }
            }

            throw new ArgumentException($"Couldn't figure out what kind of metadata selector this is: {yaml}");
        }
    }
}
