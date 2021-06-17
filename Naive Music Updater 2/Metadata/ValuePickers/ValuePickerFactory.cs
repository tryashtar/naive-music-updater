using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IValuePicker
    {
        MetadataProperty PickFrom(MetadataProperty full);
    }

    public static class ValuePickerFactory
    {
        public static IValuePicker Create(YamlNode node)
        {
            if (node is YamlScalarNode scalar)
            {
                var str = (string)scalar;
                if (str == "first")
                    return new IndexValuePicker(0, OutofBoundsDecision.Exit);
                if (str == "last")
                    return new IndexValuePicker(-1, OutofBoundsDecision.Exit);
                if (int.TryParse(str, out int index))
                    return new IndexValuePicker(index, OutofBoundsDecision.Exit);
            }
            else if (node is YamlMappingNode map)
            {
                IValuePicker base_picker = null;
                var apply = map.ParseOrDefault(
                    "then",
                    x => ValuePickerFactory.Create(x)
                );

                var ind = map.Go("index");
                if (ind != null && int.TryParse((string)ind, out int index))
                {
                    var oob = map.ParseOrDefault(
                        "out_of_bounds",
                        x => Util.ParseUnderscoredEnum<OutofBoundsDecision>((string)x),
                        OutofBoundsDecision.Exit
                    );
                    base_picker = new IndexValuePicker(index, oob);
                }

                var op = map.Go("operation");
                if (op != null)
                {
                    string operation = (string)op;
                    if (operation == "split")
                        base_picker = new SplitValuePicker(map);
                    else if (operation == "regex")
                        base_picker = new RegexValuePicker(map);
                }
                if (base_picker != null)
                {
                    if (apply != null)
                        base_picker = new ThenValuePicker(base_picker, apply);
                    return base_picker;
                }
            }
            else if (node is YamlSequenceNode list)
            {
                return new AppendingValuePicker(list.ToList(x => ValuePickerFactory.Create(x)));
            }
            throw new ArgumentException($"Can't make a value picker from {node}");
        }
    }
}
