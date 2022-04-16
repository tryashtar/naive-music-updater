using System.Diagnostics.CodeAnalysis;

namespace NaiveMusicUpdater;

public interface IMusicItemConfig
{
    string Location { get; }
    Metadata GetMetadata(IMusicItem item, Predicate<MetadataField> desired);
    CheckSelectorResults CheckSelectors();
}

public static class MusicItemConfigFactory
{
    public static IMusicItemConfig Create(string file, IMusicItem item)
    {
        var yaml = YamlHelper.ParseFile(file);
        var reverse = yaml.Go("reverse");
        if (reverse != null && item is MusicFolder folder)
        {
            var type = StringUtils.ParseUnderscoredEnum<ReversalType>(reverse.String());
            yaml = ProcessReversedConfig(folder, type, file);
        }
        return new MusicItemConfig(file, item, yaml);
    }

    private static YamlNode ProcessReversedConfig(MusicFolder folder, ReversalType type, string file)
    {
        var item_depth = folder.PathFromRoot().Count();
        var sets = new Dictionary<IMusicItem, Dictionary<MetadataField, MetadataProperty>>();
        var reverse_sets = new Dictionary<MetadataField, Dictionary<MetadataProperty, List<IMusicItem>>>();
        var checker = new MetadataEqualityChecker();
        foreach (var field in MetadataField.Values)
        {
            reverse_sets[field] = new(checker);
        }
        foreach (var song in folder.GetAllSongs())
        {
            var setter = sets[song] = new();
            var current = song.GetEmbeddedMetadata(MetadataField.All);
            var incoming = song.GetMetadata(MetadataField.All);
            foreach (var field in MetadataField.Values)
            {
                var val = incoming.Get(field);
                if (!reverse_sets[field].ContainsKey(val))
                    reverse_sets[field][val] = new();
                reverse_sets[field][val].Add(song);
            }
        }
        YamlNode ItemToPath(IMusicItem item)
        {
            return String.Join('/', item.PathFromRoot().Skip(item_depth).Select(x => x.SimpleName));
        }
        static YamlNode MetadataToNode(MetadataProperty prop)
        {
            YamlNode MetadataContentsToNode()
            {
                if (prop.Value is StringValue s)
                    return s.Value;
                if (prop.Value is ListValue l)
                    return new YamlSequenceNode(l.Values.ToArray());
                if (prop.Value is NumberValue n)
                    return n.Value.ToString();
                throw new ArgumentException();
            }
            if (prop.Mode == CombineMode.Replace)
                return MetadataContentsToNode();
            else if (prop.Mode == CombineMode.Remove)
                return new YamlMappingNode(new[] { new YamlScalarNode("mode"), new YamlScalarNode("remove") });
            else
                return new YamlMappingNode(new[] {
                    "mode", prop.Mode.ToString().ToLower(),
                    "source", MetadataContentsToNode()
                });
        }
        var songs_node = new YamlMappingNode();
        var set_all_node = new YamlSequenceNode();
        var set_node = new YamlMappingNode();
        foreach (var prop in reverse_sets)
        {
            var max_list = prop.Value.MaxBy(x => x.Value.Count);
            if (max_list.Value.Count > 1)
            {
                songs_node.Add(prop.Key.Id, MetadataToNode(max_list.Key));
            }
            foreach (var val in prop.Value)
            {
                if (val.Value != max_list.Value && val.Value.Count > 1)
                {
                    set_all_node.Add(new YamlMappingNode
                        {
                            { "names", new YamlSequenceNode(val.Value.Select(ItemToPath)) },
                            { "set", new YamlMappingNode { { prop.Key.Id, MetadataToNode(val.Key) } } },
                        });
                }
                if (val.Value.Count == 1)
                {
                    sets[val.Value[0]][prop.Key] = val.Key;
                }
            }
        }
        foreach (var set in sets)
        {
            var path = ItemToPath(set.Key);
            var spec = new YamlMappingNode();
            foreach (var field in set.Value)
            {
                var node = MetadataToNode(field.Value);
                spec.Add(field.Key.Id, node);
            }
            set_node.Add(path, spec);
        }
        var final_node = new YamlMappingNode();
        if (songs_node.Children.Count > 0)
            final_node.Add("songs", songs_node);
        if (set_all_node.Children.Count > 0)
            final_node.Add("set all", set_all_node);
        if (set_node.Children.Count > 0)
            final_node.Add("set", set_node);
        YamlHelper.SaveToFile(final_node, @"zebra.txt");
        return final_node;
    }

    private class MetadataEqualityChecker : IEqualityComparer<MetadataProperty>
    {
        public bool Equals(MetadataProperty? x, MetadataProperty? y)
        {
            if (x.Value.IsBlank && y.Value.IsBlank)
                return true;
            if (x.Value is StringValue sx && y.Value is StringValue sy)
                return sx.Value == sy.Value;
            if (x.Value is NumberValue nx && y.Value is NumberValue ny)
                return nx.Value == ny.Value;
            if (x.Value is ListValue lx && y.Value is ListValue ly)
                return lx.Values.SequenceEqual(ly.Values);
            return false;
        }

        public int GetHashCode([DisallowNull] MetadataProperty obj)
        {
            if (obj.Value is BlankValue)
                return 0;
            if (obj.Value is StringValue s)
                return s.Value.GetHashCode();
            if (obj.Value is NumberValue n)
                return n.Value.GetHashCode();
            if (obj.Value is ListValue l)
                return l.Values.GetHashCode();
            return obj.GetHashCode();
        }
    }
}

public enum ReversalType
{
    Minimal,
    Full
}
