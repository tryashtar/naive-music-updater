namespace NaiveMusicUpdater;

public class LibraryConfig
{
    private readonly string ConfigPath;
    private readonly Dictionary<Regex, string> FindReplace;
    private readonly Dictionary<string, IMetadataStrategy> NamedStrategies;
    private readonly Dictionary<string, ReplayGain> ReplayGains;
    private readonly Dictionary<Regex, KeepFrameDefinition>? KeepFrameIDs;
    private readonly List<Regex>? KeepXiphMetadata;
    private readonly List<string> SongExtensions;
    public readonly List<string> ConfigFolders;
    public readonly string LibraryFolder;
    public readonly string? LogFolder;
    public readonly ExportConfig<LyricsType>? LyricsConfig;
    public readonly ExportConfig<ChaptersType>? ChaptersConfig;
    public readonly LibraryCache Cache;

    public LibraryConfig(string file)
    {
        ConfigPath = file;
        var yaml = YamlHelper.ParseFile(file);

        LibraryFolder = Path.Combine(Path.GetDirectoryName(file),
            yaml.Go("library").String() ??
            throw new InvalidDataException("Library yaml file must specify a \"library\" folder"));
        LibraryFolder = Path.GetFullPath(LibraryFolder);
        Cache = new LibraryCache(Path.Combine(Path.GetDirectoryName(file),
            yaml.Go("cache").String() ?? ".music-cache"));
        LogFolder = yaml.Go("logs").String();
        if (LogFolder != null)
            LogFolder = Path.Combine(Path.GetDirectoryName(file), LogFolder);
        LyricsConfig = ParseExportConfig<LyricsType>(yaml.Go("lyrics"));
        ChaptersConfig = ParseExportConfig<ChaptersType>(yaml.Go("chapters"));
        FindReplace = yaml.Go("find_replace").ToDictionary(x => new Regex(x.String()), x => x.String()) ?? new();
        NamedStrategies = yaml.Go("named_strategies").ToDictionary(MetadataStrategyFactory.Create) ?? new();
        KeepFrameIDs = yaml.Go("keep", "id3v2").ToList(ParseFrameDefinition)?.ToDictionary(x => x.Id, x => x);
        KeepXiphMetadata = yaml.Go("keep", "xiph").ToListFromStrings(x => new Regex(x));
        SongExtensions =
            yaml.Go("extensions").ToListFromStrings(x => x.StartsWith('.') ? x.ToLower() : "." + x.ToLower()) ?? new();
        ReplayGains = yaml.Go("replay_gain")
            .ToDictionary(x => x.String().StartsWith('.') ? x.String() : '.' + x.String(),
                x => new ReplayGain(x["path"].String(), x["args"].String()));
        ConfigFolders = yaml.Go("config_folders").ToStringList() ?? new() { LibraryFolder };
    }

    private record KeepFrameDefinition(Regex Id, Regex[] Descriptions, bool DuplicatesAllowed);

    private KeepFrameDefinition ParseFrameDefinition(YamlNode node)
    {
        if (node is YamlScalarNode simple)
            return new KeepFrameDefinition(new Regex(simple.Value), Array.Empty<Regex>(), false);
        if (node is YamlMappingNode map)
            return new KeepFrameDefinition(new Regex(map.Go("id").String()),
                map.TryGet("desc").ToList(x => new Regex(x.String()))?.ToArray() ?? Array.Empty<Regex>(),
                map.Go("dupes").Bool() ?? false);
        throw new FormatException();
    }

    private ExportConfig<T>? ParseExportConfig<T>(YamlNode? node) where T : struct, Enum
    {
        if (node == null)
            return null;
        var path = node.Go("folder").String();
        if (path != null)
            path = Path.Combine(Path.GetDirectoryName(ConfigPath), path);
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

    public bool ShouldKeepXiph(string key)
    {
        return KeepXiphMetadata == null || KeepXiphMetadata.Any(x => x.IsMatch(key));
    }

    public string CleanName(string name)
    {
        foreach (var findrepl in FindReplace)
        {
            name = findrepl.Key.Replace(name, findrepl.Value);
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
            Logger.WriteLine("Weird characters detected, doing weird rename thingy");
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