namespace NaiveMusicUpdater;

public interface IValueOperator
{
    IValue? Apply(IMusicItem item, IValue original);
}

public static class ValueOperatorFactory
{
    public static IValueOperator Create(YamlNode yaml)
    {
        switch (yaml)
        {
            case YamlScalarNode { Value: "first" }:
                return new IndexOperator(0, OutofBoundsDecision.Exit);
            case YamlScalarNode { Value: "last" }:
                return new IndexOperator(-1, OutofBoundsDecision.Exit);
            case YamlScalarNode scalar when int.TryParse(scalar.Value, out int index):
                return new IndexOperator(index, OutofBoundsDecision.Exit);
            case YamlScalarNode { Value: "reverse" }:
                return ReverseOperator.Instance;
            case YamlMappingNode map:
            {
                var index = map.Go("index").Int();
                if (index != null)
                {
                    var oob = map.Go("out_of_bounds").ToEnum(def: OutofBoundsDecision.Exit);
                    var min_length = map.Go("min_length").Int();
                    return new IndexOperator(index.Value, oob, min_length);
                }

                var split = map.Go("split").String();
                if (split != null)
                {
                    var decision = map.Go("when_none").ToEnum(def: NoSeparatorDecision.Ignore);
                    return new SplitOperator(split, decision);
                }

                var group = map.Go("group").String();
                if (group != null)
                    return new RegexGroupOperator(group);

                var regex = map.Go("regex").NullableParse(x => new Regex(x.String()!));
                if (regex != null)
                {
                    var decision = yaml.Go("fail").ToEnum(def: MatchFailDecision.Exit);
                    return new RegexOperator(regex, decision);
                }

                var prepend = map.Go("prepend").NullableParse(ValueSourceFactory.Create);
                if (prepend != null)
                    return new AppendOperator(prepend, AppendMode.Prepend,
                        map.Go("range").NullableParse(RangeFactory.Create));

                var append = map.Go("append").NullableParse(ValueSourceFactory.Create);
                if (append != null)
                    return new AppendOperator(append, AppendMode.Append,
                        map.Go("range").NullableParse(RangeFactory.Create));

                var join = map.Go("join").NullableParse(ValueSourceFactory.Create);
                if (join != null)
                    return new JoinOperator(join);
                break;
            }
            case YamlSequenceNode sequence:
                return new MultipleOperator(sequence.ToList(ValueOperatorFactory.Create)!);
        }

        throw new ArgumentException($"Can't make value operator from {yaml}");
    }
}