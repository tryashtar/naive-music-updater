namespace NaiveMusicUpdater;

public interface IMusicItemConfig
{
    string Location { get; }
    void Apply(Metadata meta, IMusicItem item, Predicate<MetadataField> desired);
    CheckSelectorResults CheckSelectors();
}

public static class MusicItemConfigFactory
{
    public static IMusicItemConfig Create(string file, IMusicItem item)
    {
        while (true)
        {
            var yaml = TryParseFile(file);
            var reverse = yaml.Go("reverse").String();
            if (reverse != null && item is MusicFolder folder)
            {
                var type = StringUtils.ParseUnderscoredEnum<ReversalType>(reverse);
                yaml = ProcessReversedConfig(folder, type, file);
            }

            var config = new MusicItemConfig(file, item, yaml);
            if (!CheckSelectorsAndReload(config))
                return config;
        }
    }

    private static YamlNode? TryParseFile(string file)
    {
        while (true)
        {
            try
            {
                return YamlHelper.ParseFile(file);
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Error loading config file: {file}", ConsoleColor.Yellow);
                Logger.WriteLine(ex.Message, ConsoleColor.Yellow);
                if (!AskUserEdit(file))
                    throw;
            }
        }
    }

    private static bool AskUserEdit(string path)
    {
        Logger.WriteLine("Would you like to edit the file now to correct it?");
        Logger.WriteLine("Type 'Y' to edit.");
        bool reload = Console.ReadKey().Key == ConsoleKey.Y;
        if (reload)
        {
            var proc = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            proc.WaitForExit();
        }

        return reload;
    }

    private static bool CheckSelectorsAndReload(MusicItemConfig config)
    {
        var results = config.CheckSelectors();
        if (results.AnyUnused)
            Logger.WriteLine($"Problems found in config file: {config.Location}", ConsoleColor.Yellow);
        if (results.UnusedSelectors.Count > 0)
        {
            Logger.WriteLine($"Has {results.UnusedSelectors.Count} selectors that didn't find anything:",
                ConsoleColor.Yellow);
            Logger.TabIn();
            foreach (var sel in results.UnusedSelectors)
            {
                Logger.WriteLine(sel.ToString(), ConsoleColor.Yellow);
            }

            Logger.TabOut();
        }

        if (results.UnselectedItems.Count > 0)
        {
            Logger.WriteLine($"Has {results.UnselectedItems.Count} unselected items:", ConsoleColor.Yellow);
            Logger.TabIn();
            foreach (var sel in results.UnselectedItems)
            {
                Logger.WriteLine(sel.SimpleName, ConsoleColor.Yellow);
            }

            Logger.TabOut();
        }

        return results.AnyUnused && AskUserEdit(config.Location);
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

        var songs = folder.GetAllSongs().ToList();
        foreach (var song in songs)
        {
            sets[song] = new();
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

        static YamlNode ValueToNode(IValue val)
        {
            var l = val.AsList();
            if (l.Values.Count == 1)
                return l.Values[0];
            return new YamlSequenceNode(l.Values.ToArray());
        }

        YamlNode songs_node = new YamlMappingNode();
        var set_all_node = new YamlSequenceNode();
        var set_node = new YamlMappingNode();
        var order_node = new YamlSequenceNode();
        var discs_node = new YamlMappingNode();
        var discs = reverse_sets[MetadataField.Disc];
        if (discs.Count > 1)
        {
            foreach (var disc in discs.OrderBy(x => ((NumberValue)x.Key).Value))
            {
                var dn = new YamlSequenceNode(disc.Value.OrderBy(x => tracks[x]).Select(ItemToPath));
                discs_node.Add(disc.Key.AsString().Value, dn);
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

        foreach (var (field, values) in reverse_sets)
        {
            if (values.Count == 0)
                continue;
            var max_list = values.OrderBy(x => x.Value.Count).ToList();
            if ((values.Count == 1 && values.Single().Value.Count == songs.Count()) ||
                (max_list.Count > 1 && max_list[0].Value.Count > max_list[1].Value.Count))
            {
                if (max_list[0].Key.IsBlank)
                {
                    if (songs_node is YamlMappingNode map)
                        songs_node = new YamlSequenceNode(map,
                            new YamlMappingNode() { { "remove", new YamlSequenceNode() } });
                    ((YamlSequenceNode)songs_node[1]["remove"]).Add(field.Id);
                }
                else
                {
                    var add = songs_node is YamlMappingNode map ? map : (YamlMappingNode)songs_node[0];
                    add.Add(field.Id, ValueToNode(max_list[0].Key));
                }
            }

            foreach (var (val, items) in values)
            {
                if (items != max_list[0].Value && items.Count > 1)
                {
                    if (val.IsBlank)
                    {
                        set_all_node.Add(new YamlMappingNode
                        {
                            { "names", new YamlSequenceNode(items.Select(ItemToPath)) },
                            { "set", new YamlMappingNode { { "remove", new YamlSequenceNode() { field.Id } } } }
                        });
                    }
                    else
                    {
                        set_all_node.Add(new YamlMappingNode
                        {
                            { "names", new YamlSequenceNode(items.Select(ItemToPath)) },
                            { "set", new YamlMappingNode { { field.Id, ValueToNode(val) } } }
                        });
                    }
                }

                if (items.Count == 1)
                    sets[items[0]][field] = val;
            }
        }

        foreach (var (item, meta) in sets)
        {
            var path = ItemToPath(item);
            YamlNode spec = new YamlMappingNode();
            foreach (var (field, val) in meta)
            {
                if (val.IsBlank)
                {
                    if (spec is YamlMappingNode map)
                        spec = new YamlSequenceNode(map,
                            new YamlMappingNode() { { "remove", new YamlSequenceNode() } });
                    ((YamlSequenceNode)songs_node[1]["remove"]).Add(field.Id);
                }
                else
                {
                    var add = spec is YamlMappingNode map ? map : (YamlMappingNode)spec[0];
                    add.Add(field.Id, ValueToNode(val));
                }
            }

            if (spec is YamlMappingNode { Children.Count: > 0 } or YamlSequenceNode { Children.Count: > 0 })
                set_node.Add(path, spec);
        }

        var final_node = new YamlMappingNode();
        if (songs_node is YamlMappingNode { Children.Count: > 0 } or YamlSequenceNode { Children.Count: > 0 })
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