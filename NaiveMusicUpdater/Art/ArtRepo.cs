namespace NaiveMusicUpdater;

public class ArtRepo
{
    public readonly string Folder;
    private IArtCache Cache;
    private Dictionary<string, ArtConfig> ConfigCache = new();

    public ArtRepo(string folder, IArtCache cache)
    {
        Folder = folder;
        Cache = cache;
    }

    public IPicture? GetProcessed(string path)
    {
        var cached = Cache.Get(path);
        if (cached != null)
            return cached;
        var file = FindFile(path);
        if (file == null)
            return null;
        var settings = GetSettings(path);

        IPicture result = null;
        Cache.Put(path, result);
        return result;
    }

    private ProcessArtSettings GetSettings(string path)
    {
        ProcessArtSettings settings = new ProcessArtSettings();
        foreach (var config in GetConfigs(path))
        {
            foreach (var (check, apply) in config.Settings)
            {
                if (check(path))
                    settings.MergeWith(apply);
            }
        }

        return settings;
    }

    private IEnumerable<ArtConfig> GetConfigs(string path)
    {
        while (path != "")
        {
            var file = Path.Combine(Folder, path, "images.yaml");
            if (ConfigCache.TryGetValue(file, out var existing))
                yield return existing;
            var config = new ArtConfig((YamlMappingNode)YamlHelper.ParseFile(file));
            ConfigCache[file] = config;
            yield return config;
            path = Path.GetDirectoryName(path);
        }
    }

    private string? FindFile(string path)
    {
        var name = Path.Combine(Folder, path);
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(name)))
        {
            if (Path.GetFileNameWithoutExtension(file) == Path.GetFileName(name))
                return file;
        }

        return null;
    }
}