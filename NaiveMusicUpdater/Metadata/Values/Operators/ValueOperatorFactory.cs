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
            case YamlScalarNode { Value: "reverse" }:
                return ReverseOperator.Instance;
            case YamlMappingNode map:
            {
                var take = map.Go("take");
                if (take != null)
                {
                    if (take is YamlMappingNode indmap)
                    {
                        var index = indmap.Go("index").NullableStructParse(RangeFactory.Create).Value;
                        var oob = indmap.Go("out_of_bounds").ToEnum(def: OutofBoundsDecision.Exit);
                        var min_length = indmap.Go("min_length").Int();
                        return new IndexOperator(index, oob, min_length);
                    }
                    else
                        return new IndexOperator(take.NullableStructParse(RangeFactory.Create).Value,
                            OutofBoundsDecision.Exit);
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
                        map.Go("index").NullableStructParse(RangeFactory.Create));

                var append = map.Go("append").NullableParse(ValueSourceFactory.Create);
                if (append != null)
                    return new AppendOperator(append, AppendMode.Append,
                        map.Go("index").NullableStructParse(RangeFactory.Create));

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