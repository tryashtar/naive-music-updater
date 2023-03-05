namespace NaiveMusicUpdater;

public class ArtConfig
{
    private readonly ArtRepo Owner;
    public readonly List<(Predicate<string> pred, ProcessArtSettings settings)> Settings;

    public ArtConfig(ArtRepo owner, string folder, string relative)
    {
        Owner = owner;
        var node = (YamlMappingNode)YamlHelper.ParseFile(Path.Combine(folder, relative, "images.yaml"));
        Settings = new();
        var all = node.Go("all").NullableParse(LiteralOrReference);
        if (all != null)
            Settings.Add((x => true, all));
        var set_dict = node.Go("set").ToDictionary(x => x.String(), LiteralOrReference);
        if (set_dict != null)
        {
            foreach (var (name, settings) in set_dict)
            {
                Settings.Add((x => x == Path.Combine(relative, name), settings));
            }
        }

        if (node.Children.TryGetValue("set all", out var set_all))
        {
            foreach (var item in (YamlSequenceNode)set_all)
            {
                var names = set_all.Go("names").ToStringList().Select(x => Path.Combine(relative, x)).ToList();
                var set = LiteralOrReference(set_all["set"]);
                Settings.Add((x => names.Contains(x), set));
            }
        }
    }

    private ProcessArtSettings LiteralOrReference(YamlNode node)
    {
        if (node is YamlScalarNode scalar && scalar.Value != null)
            return Owner.NamedSettings[scalar.Value];
        else
            return new ProcessArtSettings((YamlMappingNode)node);
    }
}