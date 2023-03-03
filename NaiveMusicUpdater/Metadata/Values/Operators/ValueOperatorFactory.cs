namespace NaiveMusicUpdater;

public interface IValueOperator
{
    IValue Apply(IMusicItem item, IValue original);
}

public static class ValueOperatorFactory
{
    public static IValueOperator Create(YamlNode yaml)
    {
        switch (yaml)
        {
            case YamlScalarNode { Value: "first" }:
                return new IndexValueOperator(0, OutofBoundsDecision.Exit);
            case YamlScalarNode { Value: "last" }:
                return new IndexValueOperator(-1, OutofBoundsDecision.Exit);
            case YamlScalarNode scalar when int.TryParse(scalar.Value, out int index):
                return new IndexValueOperator(index, OutofBoundsDecision.Exit);
            case YamlMappingNode map:
            {
                var index = map.Go("index").Int();
                if (index != null)
                {
                    var oob = map.Go("out_of_bounds").ToEnum(def: OutofBoundsDecision.Exit);
                    var min_length = map.Go("min_length").Int();
                    return new IndexValueOperator(index.Value, oob, min_length);
                }

                var split = map.Go("split").String();
                if (split != null)
                {
                    var decision = map.Go("when_none").ToEnum(def: NoSeparatorDecision.Ignore);
                    return new SplitValueOperator(split, decision);
                }

                var group = map.Go("group").String();
                if (group != null)
                    return new RegexGroupOperator(group);

                var regex = map.Go("regex").NullableParse(x => new Regex(x.String()));
                if (regex != null)
                {
                    var decision = yaml.Go("fail").ToEnum(def: MatchFailDecision.Exit);
                    return new RegexValueOperator(regex, decision);
                }

                var prepend = map.Go("prepend").NullableParse(ValueSourceFactory.Create);
                if (prepend != null)
                    return new AppendValueOperator(prepend, AppendMode.Prepend);

                var append = map.Go("append").NullableParse(ValueSourceFactory.Create);
                if (append != null)
                    return new AppendValueOperator(append, AppendMode.Append);

                var join = map.Go("join").NullableParse(ValueSourceFactory.Create);
                if (join != null)
                    return new JoinOperator(join);
                break;
            }
            case YamlSequenceNode sequence:
                return new MultipleValueOperator(sequence.ToList(ValueOperatorFactory.Create));
        }

        throw new ArgumentException($"Can't make value operator from {yaml}");
    }
}
