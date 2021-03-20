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
                    return new FileNameSelector();
                if (val == "<exact>")
                    return new SimpleNameSelector();
                return new LiteralSelector(val);
            }
            else if (yaml is YamlMappingNode map)
            {
                var operation = (string)map.Go("operation");
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
                        var up = map.Go("up");
                        return new SimpleParentSelector(int.Parse((string)up));
                    }
                    else if (operation == "remove")
                        return new RemoveSelector();
                }
            }
            else if (yaml is YamlSequenceNode sequence)
            {
                return new ListSelector(sequence);
            }

            throw new ArgumentException($"Couldn't figure out what kind of metadata selector this is: {yaml}");
        }
    }
}
