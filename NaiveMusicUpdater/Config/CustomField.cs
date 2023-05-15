namespace NaiveMusicUpdater;

public record CustomField(MetadataField Field, string? Export, IFieldGrouping Group, bool IncludeBlanks);

public interface IFieldGrouping
{
    void SaveResults(YamlMappingNode yaml, IEnumerable<KeyValuePair<IMusicItem, IValue>> results);
}

public static class FieldGroupingFactory
{
    public static IFieldGrouping Create(YamlNode node)
    {
        var type = node.Go("type").String();
        if (type == "item")
            return new ItemFieldGrouping();
        else if (type == "value")
        {
            if (node.Go("split").Bool() ?? false)
                return new SplitValueFieldGrouping();
            else
                return new JoinedValueFieldGrouping(node.Go("separator").String() ?? ";");
        }

        throw new ArgumentException($"Can't make metadata strategy from {node}");
    }
}

public class ItemFieldGrouping : IFieldGrouping
{
    public void SaveResults(YamlMappingNode yaml, IEnumerable<KeyValuePair<IMusicItem, IValue>> results)
    {
        foreach (var (item, value) in results)
        {
            yaml.Add(item.StringPathAfterRoot(), SaveValue(value));
        }
    }

    private static YamlNode SaveValue(IValue value)
    {
        if (value.IsBlank)
            return new YamlScalarNode(null);
        if (value is ListValue list)
            return new YamlSequenceNode(list.Values.Select(x => new YamlMappingNode(x)));
        return new YamlScalarNode(value.AsString().Value);
    }
}

public class JoinedValueFieldGrouping : IFieldGrouping
{
    public readonly string Separator;

    public JoinedValueFieldGrouping(string separator)
    {
        Separator = separator;
    }

    public void SaveResults(YamlMappingNode yaml, IEnumerable<KeyValuePair<IMusicItem, IValue>> results)
    {
        var reverse_dict = new Dictionary<IValue, List<IMusicItem>>(new ValueEqualityChecker());
        foreach (var (item, value) in results)
        {
            if (!reverse_dict.ContainsKey(value))
                reverse_dict[value] = new();
            reverse_dict[value].Add(item);
        }

        foreach (var (value, list) in reverse_dict)
        {
            var vals = list.Select(x => new YamlScalarNode(x.StringPathAfterRoot())).ToList();
            string key = value.IsBlank
                ? "(blank)"
                : String.Join(Separator, value.AsList().Values);
            yaml.Add(key, vals.Count == 1 ? vals[0] : new YamlSequenceNode(vals));
        }
    }
}

public class SplitValueFieldGrouping : IFieldGrouping
{
    public void SaveResults(YamlMappingNode yaml, IEnumerable<KeyValuePair<IMusicItem, IValue>> results)
    {
        var reverse_dict = new Dictionary<string, List<IMusicItem>>();
        foreach (var (item, value) in results)
        {
            IEnumerable<string> entries = value.IsBlank ? new[] { "(blank)" } : value.AsList().Values;
            foreach (var entry in entries)
            {
                if (!reverse_dict.ContainsKey(entry))
                    reverse_dict[entry] = new();
                reverse_dict[entry].Add(item);
            }
        }

        foreach (var (value, list) in reverse_dict)
        {
            var vals = list.Select(x => new YamlScalarNode(x.StringPathAfterRoot())).ToList();
            yaml.Add(value, vals.Count == 1 ? vals[0] : new YamlSequenceNode(vals));
        }
    }
}