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
        public static MetadataSelector Create(YamlNode yaml)
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
                var picker = map.ParseOrDefault("pick", x => ValuePickerFactory.Create(x));
                MetadataSelector base_selector = null;
                var operation = (string)map.Go("operation");
                var simple_base = map.Go("base");
                if (operation != null)
                {
                    if (operation == "copy")
                        base_selector = new CopyMetadataSelector(map);
                    else if (operation == "join")
                        base_selector = new JoinOperationSelector(map);
                    else if (operation == "parent")
                    {
                        var up = map.Go("up");
                        base_selector = new SimpleParentSelector(int.Parse((string)up));
                    }
                    else if (operation == "remove")
                        base_selector = new RemoveSelector();
                }
                else if (simple_base!=null)
                {
                    base_selector = MetadataSelectorFactory.Create(simple_base);
                }
                if (base_selector != null)
                {
                    if (picker != null)
                        return new PickedSelector(base_selector, picker);
                    return base_selector;
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
