namespace NaiveMusicUpdater;

public class ArtConfig
{
    public readonly List<(Predicate<string> pred, ProcessArtSettings settings)> Settings;

    public ArtConfig(YamlMappingNode node)
    {
        Settings = new();
        var all = node.Go("all").NullableParse(x => new ProcessArtSettings((YamlMappingNode)x));
        if (all != null)
            Settings.Add((x => true, all));
        var set_dict = node.Go("set").ToDictionary(x => x.String(), x => new ProcessArtSettings((YamlMappingNode)x));
        if (set_dict != null)
        {
            foreach (var (name, settings) in set_dict)
            {
                Settings.Add((x => x == name, settings));
            }
        }

        if (node.Children.TryGetValue("set all", out var set_all))
        {
            foreach (var item in (YamlSequenceNode)set_all)
            {
                var names = set_all.Go("names").ToStringList();
                var set = new ProcessArtSettings((YamlMappingNode)set_all["set"]);
                Settings.Add((x => names.Contains(x), set));
            }
        }
    }
}