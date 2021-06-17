using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace NaiveMusicUpdater
{
    public interface IValueOperator
    {
        IValue Apply(IValue original);
    }

    public static class ValueOperatorFactory
    {
        public static IValueOperator Create(YamlNode node)
        {
            if (node is YamlScalarNode scalar)
            {
                var str = (string)scalar;
                if (str == "first")
                    return new IndexValueOperator(0, OutofBoundsDecision.Exit);
                if (str == "last")
                    return new IndexValueOperator(-1, OutofBoundsDecision.Exit);
                if (int.TryParse(str, out int index))
                    return new IndexValueOperator(index, OutofBoundsDecision.Exit);
            }
            else if (node is YamlMappingNode map)
            {
                var ind = map.Go("index");
                if (ind != null && int.TryParse((string)ind, out int index))
                {
                    var oob = map.ParseOrDefault(
                        "out_of_bounds",
                        x => Util.ParseUnderscoredEnum<OutofBoundsDecision>((string)x),
                        OutofBoundsDecision.Exit
                    );
                    return new IndexValueOperator(index, oob);
                }

                var op = map.Go("operation");
                if (op != null)
                {
                    string operation = (string)op;
                    if (operation == "split")
                        return new SplitValueOperator(map);
                    else if (operation == "regex")
                        return new RegexValueOperator(map);
                }

                var group = map.Go("group");
                if (group != null)
                    return new RegexGroupOperator((string)group);
            }
            throw new ArgumentException($"Can't make a value operator from {node}");
        }
    }
}
