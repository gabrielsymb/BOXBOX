using Godot;

namespace Boxbox.Presentation.OpenWorld;

public enum WorldAssetCategory
{
    CommercialBuildings,
    ResidentialBuildings,
    Nature,
    Vehicles,
    LotProps,
    StreetFurniture,
    RoadModules
    , NpcCharacters
}

/// <summary>
/// Discovers imported scenes through ResourceLoader so the same catalog works in
/// the editor and in exported PCKs. Results are sorted because directory order is
/// platform-dependent and world generation must remain deterministic.
/// </summary>
public sealed class WorldAssetCatalog
{
    private const string AssetsRoot = "res://Assets/";
    private const string CommercialBuildingsRoot = AssetsRoot + "World/Buildings/Commercial/";
    private const string ResidentialBuildingsRoot = AssetsRoot + "World/Buildings/Suburban/";
    private const string NatureRoot = AssetsRoot + "World/Nature/";
    private const string VehiclesRoot = AssetsRoot + "World/Vehicles/";
    private const string LotPropsRoot = AssetsRoot + "World/Props/";
    private const string StreetFurnitureRoot = AssetsRoot + "World/Roads/StreetFurniture/";
    private const string RoadModulesRoot = AssetsRoot + "World/Roads/";
    private const string NpcCharactersRoot = AssetsRoot + "Characters/NPCs/";

    private static readonly Lazy<WorldAssetCatalog> LazyCurrent = new(() => new WorldAssetCatalog());
    private readonly IReadOnlyDictionary<WorldAssetCategory, IReadOnlyList<string>> _paths;

    public static WorldAssetCatalog Current => LazyCurrent.Value;
    public IReadOnlyList<string> AllPaths { get; }

    private WorldAssetCatalog()
    {
        _paths = new Dictionary<WorldAssetCategory, IReadOnlyList<string>>
        {
            [WorldAssetCategory.CommercialBuildings] = DiscoverScenes(CommercialBuildingsRoot),
            [WorldAssetCategory.ResidentialBuildings] = DiscoverScenes(ResidentialBuildingsRoot),
            [WorldAssetCategory.Nature] = DiscoverScenes(NatureRoot),
            [WorldAssetCategory.Vehicles] = DiscoverScenes(VehiclesRoot),
            [WorldAssetCategory.LotProps] = DiscoverScenes(LotPropsRoot),
            [WorldAssetCategory.StreetFurniture] = DiscoverScenes(StreetFurnitureRoot),
            [WorldAssetCategory.RoadModules] = DiscoverScenes(RoadModulesRoot, recursive: false)
            , [WorldAssetCategory.NpcCharacters] = DiscoverScenes(NpcCharactersRoot)
        };
        AllPaths = DiscoverScenes(AssetsRoot);

        foreach (var (category, paths) in _paths)
        {
            if (paths.Count == 0)
                throw new InvalidOperationException($"No imported scenes found for {category}.");
        }
    }

    public IReadOnlyList<string> Get(WorldAssetCategory category) => _paths[category];

    public string Pick(WorldAssetCategory category, uint seed)
    {
        var paths = Get(category);
        return paths[(int)(seed % (uint)paths.Count)];
    }

    public string PickByStemContains(WorldAssetCategory category, uint seed, params string[] tokens)
    {
        var compatible = Get(category).Where(path =>
        {
            var stem = System.IO.Path.GetFileNameWithoutExtension(path);
            return tokens.Any(token => stem.Contains(token, StringComparison.OrdinalIgnoreCase));
        }).ToArray();
        if (compatible.Length == 0)
            throw new InvalidOperationException($"No compatible {category} asset matched: {string.Join(", ", tokens)}");
        return compatible[(int)(seed % (uint)compatible.Length)];
    }

    public string PickBuilding(DistrictStyle district, uint seed)
    {
        if (district == DistrictStyle.Commercial)
            return Pick(WorldAssetCategory.CommercialBuildings, seed);
        if (district is DistrictStyle.Residential or DistrictStyle.Green)
            return Pick(WorldAssetCategory.ResidentialBuildings, seed);

        var commercial = Get(WorldAssetCategory.CommercialBuildings);
        var residential = Get(WorldAssetCategory.ResidentialBuildings);
        var combinedIndex = (int)(seed % (uint)(commercial.Count + residential.Count));
        return combinedIndex < commercial.Count
            ? commercial[combinedIndex]
            : residential[combinedIndex - commercial.Count];
    }

    public string RequireByStem(WorldAssetCategory category, string stem)
    {
        var path = Get(category).FirstOrDefault(candidate =>
            string.Equals(System.IO.Path.GetFileNameWithoutExtension(candidate), stem,
                StringComparison.OrdinalIgnoreCase));
        return path ?? throw new InvalidOperationException(
            $"Required {category} asset with stem '{stem}' was not discovered.");
    }

    private static IReadOnlyList<string> DiscoverScenes(string directory, bool recursive = true)
    {
        var results = new List<string>();
        DiscoverInto(NormalizeDirectory(directory), recursive, results);
        results.Sort(StringComparer.Ordinal);
        return results;
    }

    private static void DiscoverInto(string directory, bool recursive, ICollection<string> results)
    {
        foreach (var entry in ResourceLoader.ListDirectory(directory))
        {
            var name = entry.ToString();
            if (name.EndsWith('/'))
            {
                if (recursive) DiscoverInto(directory + name, true, results);
                continue;
            }

            if (name.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
                results.Add(directory + name);
        }
    }

    private static string NormalizeDirectory(string directory) =>
        directory.EndsWith('/') ? directory : directory + "/";
}
