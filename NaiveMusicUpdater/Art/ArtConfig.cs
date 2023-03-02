namespace NaiveMusicUpdater;

public class ArtConfig
{
    private readonly string RelativePath;
    public readonly List<(Predicate<string> pred, ProcessArtSettings settings)> Settings;

    public ArtConfig(string folder, string relative)
    {
        RelativePath = relative;
        var node = (YamlMappingNode)YamlHelper.ParseFile(Path.Combine(folder, relative, "images.config"));
        Settings = new();
        var all = node.Go("all").NullableParse(x => new ProcessArtSettings((YamlMappingNode)x));
        if (all != null)
            Settings.Add((x => true, all));
        var set_dict = node.Go("set").ToDictionary(x => x.String(), x => new ProcessArtSettings((YamlMappingNode)x));
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
                var set = new ProcessArtSettings((YamlMappingNode)set_all["set"]);
                Settings.Add((x => names.Contains(x), set));
            }
        }
    }
}