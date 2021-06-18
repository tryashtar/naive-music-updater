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
                if (scalar.Value == "first")
                    return new IndexValueOperator(0, OutofBoundsDecision.Exit);
                if (scalar.Value == "last")
                    return new IndexValueOperator(-1, OutofBoundsDecision.Exit);
                if (int.TryParse(scalar.Value, out int index))
                    return new IndexValueOperator(index, OutofBoundsDecision.Exit);
            }
            else if (node is YamlMappingNode map)
            {
                var index = map.Go("index").Int();
                if (index != null)
                {
                    var oob = map.Go("out_of_bounds").ToEnum(def: OutofBoundsDecision.Exit);
                    return new IndexValueOperator(index.Value, oob);
                }

                var operation = map.Go("operation").ToEnum<ValueOperatorType>();
                if (operation != null)
                {
                    if (operation == ValueOperatorType.Split)
                        return new SplitValueOperator(map);
                    else if (operation == ValueOperatorType.Regex)
                        return new RegexValueOperator(map);
                }

                var group = map.Go("group").String();
                if (group != null)
                    return new RegexGroupOperator(group);
            }
            else if (node is YamlSequenceNode sequence)
                return new MultipleValueOperator(sequence.ToList(x => ValueOperatorFactory.Create(x)));
            throw new ArgumentException($"Can't make a value operator from {node}");
        }
    }

    public enum ValueOperatorType
    {
        Split,
        Regex
    }
}
