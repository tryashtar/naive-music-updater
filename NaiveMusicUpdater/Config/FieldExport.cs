using YamlDotNet.Core;

namespace NaiveMusicUpdater;

public interface IFieldExport
{
    void Remember(IMusicItem item, Metadata data);
    YamlMappingNode Export();
}

public static class FieldExportFactory
{
    public static IFieldExport Create(YamlNode node)
    {
        var blanks = node.Go("blanks").Bool() ?? false;
        var key = node.Go("key").NullableParse(x => MetadataField.FromID(x.String()));
        if (key != null)
        {
            var split = node.Go("split").Bool() ?? false;
            return new FieldKeyExport(key, split, blanks);
        }

        var value = node.Go("values");
        if (value != null)
        {
            Predicate<MetadataField> select = value is YamlScalarNode { Value: "*" }
                ? MetadataField.All
                : x => value.ToListFromStrings(MetadataField.FromID).Contains(x);
            return new ItemKeyExport(select, blanks);
        }

        throw new ArgumentException($"Can't make field export from {node}");
    }
}

public class FieldKeyExport : IFieldExport
{
    public readonly MetadataField Field;
    private readonly bool Split;
    private readonly bool Blanks;
    private readonly Dictionary<string, List<IMusicItem>> History = new();

    public FieldKeyExport(MetadataField field, bool split, bool blanks)
    {
        Field = field;
        Split = split;
        Blanks = blanks;
    }

    public void Remember(IMusicItem item, Metadata data)
    {
        var value = data.Get(Field);
        if (value.IsBlank)
        {
            if (!Blanks)
                return;
            value = new StringValue("(blank)");
        }

        IEnumerable<string> list = Split ? value.AsList().Values : new[] { String.Join("; ", value.AsList().Values) };
        foreach (var sub in list)
        {
            if (!History.TryGetValue(sub, out var entries))
            {
                entries = new();
                History[sub] = entries;
            }

            entries.Add(item);
        }
    }

    public YamlMappingNode Export()
    {
        var node = new YamlMappingNode();
        foreach (var (key, values) in History)
        {
            if (values.Count == 1)
                node.Add(key, values[0].StringPathAfterRoot());
            else
                node.Add(key, values.Select(x => x.StringPathAfterRoot()).ToArray());
        }

        return node;
    }
}

public class ItemKeyExport : IFieldExport
{
    public readonly Predicate<MetadataField> FieldCheck;
    private readonly bool Blanks;
    private readonly Dictionary<IMusicItem, Dictionary<MetadataField, IValue>> History = new();

    public ItemKeyExport(Predicate<MetadataField> fields, bool blanks)
    {
        FieldCheck = fields;
        Blanks = blanks;
    }

    public void Remember(IMusicItem item, Metadata data)
    {
        if (!History.TryGetValue(item, out var dict))
        {
            dict = new();
            History[item] = dict;
        }

        foreach (var (field, value) in data.SavedFields)
        {
            if (!FieldCheck(field))
                continue;
            if (value.IsBlank && !Blanks)
                continue;
            dict[field] = value;
        }
    }

    public YamlMappingNode Export()
    {
        var node = new YamlMappingNode();
        foreach (var (key, fields) in History)
        {
            if (fields.Count == 0)
                continue;
            var subnode = new YamlMappingNode();
            foreach (var (field, value) in fields)
            {
                YamlNode add;
                if (value.IsBlank)
                    add = new YamlScalarNode("null");
                else
                {
                    var list = value.AsList().Values;
                    if (list.Count == 1)
                        add = new YamlScalarNode(list[0])
                            { Style = list[0].Contains('\n') ? ScalarStyle.Literal : ScalarStyle.Any };
                    else
                        add = list.ToArray();
                }

                subnode.Add(field.Id, add);
            }

            node.Add(key.StringPathAfterRoot(), subnode);
        }

        return node;
    }
}