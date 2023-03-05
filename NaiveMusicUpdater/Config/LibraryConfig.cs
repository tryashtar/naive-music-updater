namespace NaiveMusicUpdater;

public class LibraryConfig
{
    private readonly string ConfigPath;
    private readonly Dictionary<Regex, string> FindReplace;
    private readonly Dictionary<string, IMetadataStrategy> NamedStrategies;
    private readonly Dictionary<string, ReplayGain> ReplayGains;
    private readonly Dictionary<Regex, KeepFrameDefinition>? KeepFrameIDs;
    private readonly List<Regex>? KeepXiphMetadata;
    private readonly List<Regex>? KeepApeMetadata;
    private readonly List<string> SongExtensions;
    public readonly List<string> ConfigFolders;
    public readonly string LibraryFolder;
    public readonly string? LogFolder;
    public readonly ExportConfig<LyricsType>? LyricsConfig;
    public readonly ExportConfig<ChaptersType>? ChaptersConfig;
    public readonly IFileDateCache Cache;
    public readonly ArtRepo? ArtTemplates;

    public LibraryConfig(string file)
    {
        ConfigPath = file;
        var info = new FileInfo(ConfigPath);
        if (info.LinkTarget != null)
            ConfigPath = info.LinkTarget;
        var yaml = YamlHelper.ParseFile(ConfigPath);

        LibraryFolder = ParsePath(yaml.Go("library")) ??
                        throw new InvalidDataException("Library yaml file must specify a \"library\" folder");
        LibraryFolder = Path.GetFullPath(LibraryFolder);
#if DEBUG
        Cache = new DebugFileDateCache(File.Exists("debug_check.txt")
            ? File.ReadLines("debug_check.txt").ToList()
            : new());
#else
        var cachepath = ParsePath(yaml.Go("cache"));
        Cache = cachepath != null ? new FileDateCache(cachepath) : new MemoryFileDateCache();
#endif
        LogFolder = ParsePath(yaml.Go("logs"));
        LyricsConfig = ParseExportConfig<LyricsType>(yaml.Go("lyrics"));
        ChaptersConfig = ParseExportConfig<ChaptersType>(yaml.Go("chapters"));
        string? template_folder = ParsePath(yaml.Go("art", "templates"));
        if (template_folder != null)
        {
            string? cache_folder = ParsePath(yaml.Go("art", "cache"));
            IArtCache cache = cache_folder != null
                ? new DiskArtCache(cache_folder)
                : new MemoryArtCache();
            string? ico_folder = ParsePath(yaml.Go("art", "icons"));
            var named =
                yaml.Go("art", "named_settings").ToDictionary(x => new ProcessArtSettings((YamlMappingNode)x)) ?? new();
            ArtTemplates = new(template_folder, cache, Cache, ico_folder, named);
        }

        FindReplace = yaml.Go("find_replace").ToDictionary(x => new Regex(x.String()), x => x.String()) ?? new();
        NamedStrategies = yaml.Go("named_strategies").ToDictionary(MetadataStrategyFactory.Create) ?? new();
        KeepFrameIDs = yaml.Go("keep", "id3v2").ToList(ParseFrameDefinition)?.ToDictionary(x => x.Id, x => x);
        KeepXiphMetadata = yaml.Go("keep", "xiph").ToListFromStrings(x => new Regex(x));
        KeepApeMetadata = yaml.Go("keep", "ape").ToListFromStrings(x => new Regex(x));
        SongExtensions =
            yaml.Go("extensions").ToListFromStrings(x => x.StartsWith('.') ? x.ToLower() : "." + x.ToLower()) ?? new();
        ReplayGains = yaml.Go("replay_gain")
            .ToDictionary(x => x.String().StartsWith('.') ? x.String() : '.' + x.String(),
                x => new ReplayGain(x["path"].String(), x["args"].String()));
        ConfigFolders = yaml.Go("config_folders").ToStringList() ?? new() { LibraryFolder };
    }

    private string? ParsePath(YamlNode? node)
    {
        return node?.NullableParse(x => Path.Combine(Path.GetDirectoryName(ConfigPath), x.String()));
    }

    private record KeepFrameDefinition(Regex Id, Regex[] Descriptions, bool DuplicatesAllowed);

    private static KeepFrameDefinition ParseFrameDefinition(YamlNode node)
    {
        return node switch
        {
            YamlScalarNode simple => new KeepFrameDefinition(new Regex(simple.Value), Array.Empty<Regex>(), false),
            YamlMappingNode map => new KeepFrameDefinition(new Regex(map.Go("id").String()),
                map.TryGet("desc").ToList(x => new Regex(x.String()))?.ToArray() ?? Array.Empty<Regex>(),
                map.Go("dupes").Bool() ?? false),
            _ => throw new ArgumentException($"Can't make frame definition from {node}")
        };
    }

    private ExportConfig<T>? ParseExportConfig<T>(YamlNode? node) where T : struct, Enum
    {
        if (node == null)
            return null;
        var path = ParsePath(node.Go("folder"));
        var priority = node.Go("priority").ToList<T>(x =>
                               x.ToEnum<T>().Value)
                           ?.ToArray() ??
                       Array.Empty<T>();
        var dict = node.Go("config")
                       .ToDictionary<T, ExportOption>(
                           x => x.ToEnum<T>().Value,
                           x => x.ToEnum<ExportOption>().Value) ??
                   new();
        foreach (var entry in Enum.GetValues<T>())
        {
            if (!dict.ContainsKey(entry))
                dict.Add(entry, ExportOption.Ignore);
        }

        return new ExportConfig<T>(path ?? LibraryFolder, priority, dict);
    }

    public bool IsSongFile(string file)
    {
        string extension = Path.GetExtension(file).ToLower();
        return SongExtensions.Contains(extension);
    }

    public IMetadataStrategy GetNamedStrategy(string name)
    {
        return NamedStrategies[name];
    }

    public (IEnumerable<Frame> keep, IEnumerable<Frame> remove) DecideFrames(TagLib.Id3v2.Tag tag)
    {
        if (KeepFrameIDs == null)
            return (tag.GetFrames(), Enumerable.Empty<Frame>());
        var remove = new List<Frame>();
        var frame_types = tag.GetFrames().GroupBy(x => x.FrameId.ToString()).ToList();
        foreach (var group in frame_types)
        {
            var match = KeepFrameIDs.Keys.FirstOrDefault(x => x.IsMatch(group.Key));
            if (match == null)
                remove.AddRange(group);
            else
            {
                var definition = KeepFrameIDs[match];
                IEnumerable<Frame> allowed = group;
                if (group.Key == "TXXX")
                    allowed = group.OfType<UserTextInformationFrame>()
                        .Where(x => definition.Descriptions.Any(y => y.IsMatch(x.Description)));
                if (allowed != group)
                    remove.AddRange(group.Except(allowed));
                if (!definition.DuplicatesAllowed && allowed.Count() > 1)
                    remove.AddRange(allowed.Skip(1));
            }
        }

        return (frame_types.SelectMany(x => x).Except(remove), remove);
    }

    public bool ShouldKeepFrame(string id)
    {
        return KeepFrameIDs == null || KeepFrameIDs.Keys.Any(x => x.IsMatch(id));
    }

    public bool ShouldKeepXiph(string key)
    {
        return KeepXiphMetadata == null || KeepXiphMetadata.Any(x => x.IsMatch(key));
    }

    public bool ShouldKeepApe(string key)
    {
        return KeepApeMetadata == null || KeepApeMetadata.Any(x => x.IsMatch(key));
    }

    public string CleanName(string name)
    {
        foreach (var (find, repl) in FindReplace)
        {
            name = find.Replace(name, repl);
        }

        return name;
    }

    public bool NormalizeAudio(Song song)
    {
        if (!ReplayGains.TryGetValue(Path.GetExtension(song.Location), out var relevant))
            return true;
        string location = song.Location;
        bool abnormal_chars = song.Location.Any(x => x > 255);
        string temp_file = Path.Combine(Path.GetTempPath(), "temp-song" + Path.GetExtension(song.Location));
        string text_file = Path.Combine(Path.GetDirectoryName(song.Location)!, "temp-song-original.txt");
        if (abnormal_chars)
        {
            Logger.WriteLine("Weird characters detected, doing weird rename thingy", ConsoleColor.Yellow);
            File.WriteAllText(text_file, song.Location + "\n" + temp_file);
            location = temp_file;
            if (File.Exists(location))
                throw new InvalidOperationException("That's not supposed to be there...");
            File.Move(song.Location, location);
        }

        var process = new Process()
        {
            StartInfo = new ProcessStartInfo(Environment.ExpandEnvironmentVariables(relevant.Path),
                $"{relevant.Args} \"{location}\"") { UseShellExecute = false }
        };
        process.Start();
        process.WaitForExit();
        if (abnormal_chars)
        {
            File.Move(location, song.Location);
            File.Delete(text_file);
        }

        return process.ExitCode == 0;
    }
}

public record ReplayGain(string Path, string Args);