namespace NaiveMusicUpdater;

public class ArtConfig
{
    private readonly ArtRepo Owner;
    public readonly List<(Predicate<string> pred, ProcessArtSettings settings)> Settings;

    public ArtConfig(ArtRepo owner, string folder, string relative)
    {
        Owner = owner;
        var node = (YamlMappingNode)YamlHelper.ParseFile(Path.Combine(folder, relative, "images.yaml"))!;
        Settings = new();

        void add_all(Predicate<string> pred, IEnumerable<ProcessArtSettings> settings)
        {
            foreach (var set in settings)
            {
                Settings.Add((pred, set));
            }
        }

        var all = node.Go("all").NullableParse(LiteralOrReference);
        if (all != null)
            add_all(_ => true, all);

        if (node.Children.TryGetValue("set all", out var set_all))
        {
            foreach (var item in (YamlSequenceNode)set_all)
            {
                var names = item.Go("names").ToStringList()!.Select(x => Path.Combine(relative, x)).ToList();
                var set = LiteralOrReference(item["set"]);
                add_all(x => names.Contains(x), set);
            }
        }

        var set_dict = node.Go("set").ToDictionary(x => x.String()!, LiteralOrReference);
        if (set_dict != null)
        {
            foreach (var (name, settings) in set_dict)
            {
                add_all(x => x == Path.Combine(relative, name), settings);
            }
        }
    }

    private IEnumerable<ProcessArtSettings> LiteralOrReference(YamlNode node)
    {
        IEnumerable<YamlNode> list = node is YamlSequenceNode seq ? seq.Children : new[] { node };
        foreach (var child in list)
        {
            if (child is YamlScalarNode { Value: not null } scalar)
                yield return Owner.NamedSettings[scalar.Value];
            if (child is YamlMappingNode map)
                yield return new ProcessArtSettings(map);
        }
    }
}