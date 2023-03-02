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
        var sets = new Dictionary<IMusicItem, Dictionary<MetadataField, IValue>>();
        var tracks = new Dictionary<IMusicItem, uint>();
        var reverse_sets = new Dictionary<MetadataField, Dictionary<IValue, List<IMusicItem>>>();
        var checker = ValueEqualityChecker.Instance;
        foreach (var field in MetadataField.Values)
        {
            reverse_sets[field] = new(checker);
        }
        var songs = folder.GetAllSongs();
        foreach (var song in songs)
        {
            var setter = sets[song] = new();
            var current = song.GetEmbeddedMetadata(MetadataField.All);
            var incoming = song.GetMetadata(MetadataField.All);
            foreach (var field in MetadataField.Values)
            {
                var val = current.Get(field);
                if (type == ReversalType.Full || !checker.Equals(val, incoming.Get(field)))
                {
                    if (!reverse_sets[field].ContainsKey(val))
                        reverse_sets[field][val] = new();
                    reverse_sets[field][val].Add(song);
                }
                if (field == MetadataField.Track && val is NumberValue n)
                    tracks[song] = n.Value;
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
                var l = prop.Value.AsList();
                if (l.Values.Count == 1)
                    return l.Values[0];
                return new YamlSequenceNode(l.Values.ToArray());
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
        var order_node = new YamlSequenceNode();
        var discs_node = new YamlMappingNode();
        var discs = reverse_sets[MetadataField.Disc];
        if (discs.Count > 1)
        {
            foreach (var disc in discs.OrderBy(x => ((NumberValue)x.Key.Value).Value))
            {
                var dn = new YamlSequenceNode(disc.Value.OrderBy(x => tracks[x]).Select(ItemToPath));
                discs_node.Add(disc.Key.Value.AsString().Value, dn);
            }
            reverse_sets.Remove(MetadataField.Track);
            reverse_sets.Remove(MetadataField.TrackTotal);
            reverse_sets.Remove(MetadataField.Disc);
            reverse_sets.Remove(MetadataField.DiscTotal);
        }
        else
        {
            foreach (var track in tracks.OrderBy(x => x.Value))
            {
                order_node.Add(ItemToPath(track.Key));
            }
            if (tracks.Count == songs.Count())
            {
                reverse_sets.Remove(MetadataField.Track);
                reverse_sets.Remove(MetadataField.TrackTotal);
            }
        }
        foreach (var prop in reverse_sets)
        {
            if (prop.Value.Count == 0)
                continue;
            var max_list = prop.Value.OrderBy(x => x.Value.Count).ToList();
            if ((prop.Value.Count == 1 && prop.Value.Single().Value.Count == songs.Count()) || (max_list.Count > 1 && max_list[0].Value.Count > max_list[1].Value.Count))
            {
                songs_node.Add(prop.Key.Id, MetadataToNode(max_list[0].Key));
            }
            foreach (var val in prop.Value)
            {
                if (val.Value != max_list[0].Value && val.Value.Count > 1)
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
            if (spec.Children.Count > 0)
                set_node.Add(path, spec);
        }
        var final_node = new YamlMappingNode();
        if (songs_node.Children.Count > 0)
            final_node.Add("songs", songs_node);
        if (set_all_node.Children.Count > 0)
            final_node.Add("set all", set_all_node);
        if (set_node.Children.Count > 0)
            final_node.Add("set", set_node);
        if (order_node.Children.Count > 0)
            final_node.Add("order", order_node);
        if (discs_node.Children.Count > 0)
            final_node.Add("discs", discs_node);
        YamlHelper.SaveToFile(final_node, file);
        return final_node;
    }
}

public enum ReversalType
{
    Minimal,
    Full
}
