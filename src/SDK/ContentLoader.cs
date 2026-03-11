using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DINOForge.SDK.Dependencies;
using DINOForge.SDK.Models;
using DINOForge.SDK.Registry;
using DINOForge.SDK.Validation;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DINOForge.SDK
{
    /// <summary>
    /// Orchestrates loading content packs from disk into the registry system.
    /// Handles manifest parsing, schema validation, YAML deserialization, and registration.
    /// </summary>
    public class ContentLoader
    {
        private readonly RegistryManager _registryManager;
        private readonly ISchemaValidator? _schemaValidator;
        private readonly Action<string> _log;
        private readonly PackLoader _packLoader;
        private readonly PackDependencyResolver _dependencyResolver;
        private readonly IDeserializer _deserializer;
        private readonly List<StatOverrideDefinition> _loadedOverrides = new List<StatOverrideDefinition>();

        /// <summary>
        /// All stat override definitions loaded from packs.
        /// </summary>
        public IReadOnlyList<StatOverrideDefinition> LoadedOverrides => _loadedOverrides;

        /// <summary>
        /// Mapping from content type keys (as used in pack.yaml loads section)
        /// to the schema name used for validation.
        /// </summary>
        private static readonly Dictionary<string, string> ContentTypeToSchema = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "units", "unit" },
            { "buildings", "building" },
            { "factions", "faction" },
            { "weapons", "weapon" },
            { "projectiles", "projectile" },
            { "doctrines", "doctrine" },
            { "stats", "stat-override" }
        };

        /// <summary>
        /// Initializes a new <see cref="ContentLoader"/>.
        /// </summary>
        /// <param name="registryManager">The registry manager to populate.</param>
        /// <param name="schemaValidator">Optional schema validator. Pass null to skip validation.</param>
        /// <param name="log">Logging callback. Pass null for no logging.</param>
        public ContentLoader(RegistryManager registryManager, ISchemaValidator? schemaValidator = null, Action<string>? log = null)
        {
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
            _schemaValidator = schemaValidator;
            _log = log ?? (_ => { });
            _packLoader = new PackLoader();
            _dependencyResolver = new PackDependencyResolver();
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        /// <summary>
        /// Loads a single pack from a directory containing pack.yaml.
        /// </summary>
        /// <param name="packDirectory">Path to the pack directory.</param>
        /// <returns>Result indicating success or failure with errors.</returns>
        public ContentLoadResult LoadPack(string packDirectory)
        {
            List<string> errors = new List<string>();

            // 1. Find and parse manifest
            string manifestPath = Path.Combine(packDirectory, "pack.yaml");
            if (!File.Exists(manifestPath))
            {
                return ContentLoadResult.Failure(new List<string>
                {
                    $"Pack manifest not found: {manifestPath}"
                }.AsReadOnly());
            }

            PackManifest manifest;
            try
            {
                manifest = _packLoader.LoadFromFile(manifestPath);
                _log($"[ContentLoader] Loaded manifest for pack '{manifest.Id}'");
            }
            catch (Exception ex)
            {
                return ContentLoadResult.Failure(new List<string>
                {
                    $"Failed to parse manifest at {manifestPath}: {ex.Message}"
                }.AsReadOnly());
            }

            // 2. Walk content types from the Loads section
            if (manifest.Loads != null)
            {
                LoadContentType(packDirectory, manifest, "units", manifest.Loads.Units, errors);
                LoadContentType(packDirectory, manifest, "buildings", manifest.Loads.Buildings, errors);
                LoadContentType(packDirectory, manifest, "factions", manifest.Loads.Factions, errors);
                LoadContentType(packDirectory, manifest, "weapons", manifest.Loads.Weapons, errors);
                LoadContentType(packDirectory, manifest, "doctrines", manifest.Loads.Doctrines, errors);

                // 3. Load stat overrides from Overrides section or conventional stats/ subdirectory
                if (manifest.Overrides?.Stats != null && manifest.Overrides.Stats.Count > 0)
                {
                    LoadContentType(packDirectory, manifest, "stats", manifest.Overrides.Stats, errors);
                }
                else
                {
                    // Scan conventional stats/ subdirectory
                    LoadContentType(packDirectory, manifest, "stats", null, errors);
                }
            }
            else
            {
                // Scan conventional subdirectories if Loads is not specified
                ScanConventionalDirectories(packDirectory, manifest, errors);
            }

            if (errors.Count > 0)
            {
                return ContentLoadResult.Partial(
                    new List<string> { manifest.Id }.AsReadOnly(),
                    errors.AsReadOnly());
            }

            return ContentLoadResult.Success(new List<string> { manifest.Id }.AsReadOnly());
        }

        /// <summary>
        /// Discovers and loads all packs in a root directory, resolving dependencies.
        /// </summary>
        /// <param name="packsRootDirectory">Root directory containing pack subdirectories.</param>
        /// <returns>Aggregate result of loading all packs.</returns>
        public ContentLoadResult LoadPacks(string packsRootDirectory)
        {
            if (!Directory.Exists(packsRootDirectory))
            {
                return ContentLoadResult.Failure(new List<string>
                {
                    $"Packs directory not found: {packsRootDirectory}"
                }.AsReadOnly());
            }

            List<string> errors = new List<string>();
            Dictionary<string, List<string>> errorsByPack = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            List<(string Directory, PackManifest Manifest)> manifests = new List<(string Directory, PackManifest Manifest)>();

            // 1. Discover all pack directories
            foreach (string dir in Directory.GetDirectories(packsRootDirectory))
            {
                string manifestPath = Path.Combine(dir, "pack.yaml");
                if (!File.Exists(manifestPath))
                    continue;

                try
                {
                    PackManifest manifest = _packLoader.LoadFromFile(manifestPath);
                    manifests.Add((dir, manifest));
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to parse manifest in {dir}: {ex.Message}");
                }
            }

            if (manifests.Count == 0)
            {
                if (errors.Count > 0)
                    return ContentLoadResult.Failure(errors.AsReadOnly());
                return ContentLoadResult.Success(new List<string>().AsReadOnly());
            }

            // 2. Resolve dependencies and compute load order
            DependencyResult depResult = _dependencyResolver.ComputeLoadOrder(
                manifests.Select(m => m.Manifest));

            if (!depResult.IsSuccess)
            {
                errors.AddRange(depResult.Errors);
                return ContentLoadResult.Failure(errors.AsReadOnly());
            }

            // 3. Check for conflicts
            IReadOnlyList<string> conflicts = _dependencyResolver.DetectConflicts(
                manifests.Select(m => m.Manifest));
            if (conflicts.Count > 0)
            {
                errors.AddRange(conflicts);
            }

            // 4. Load packs in dependency order
            List<string> loadedPacks = new List<string>();
            Dictionary<string, string> dirByPackId = manifests.ToDictionary(
                m => m.Manifest.Id,
                m => m.Directory,
                StringComparer.OrdinalIgnoreCase);

            foreach (PackManifest orderedManifest in depResult.LoadOrder)
            {
                if (!dirByPackId.TryGetValue(orderedManifest.Id, out string? packDir))
                    continue;

                ContentLoadResult packResult = LoadPack(packDir);
                loadedPacks.AddRange(packResult.LoadedPacks);
                if (!packResult.IsSuccess)
                {
                    errors.AddRange(packResult.Errors);
                    // Track errors per pack
                    if (!errorsByPack.ContainsKey(orderedManifest.Id))
                        errorsByPack[orderedManifest.Id] = new List<string>();
                    errorsByPack[orderedManifest.Id].AddRange(packResult.Errors);
                }
            }

            if (errors.Count > 0)
            {
                // Convert to readonly lists in the dictionary
                var readonlyErrorsByPack = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in errorsByPack)
                {
                    readonlyErrorsByPack[kvp.Key] = kvp.Value.AsReadOnly();
                }

                return ContentLoadResult.Partial(loadedPacks.AsReadOnly(), errors.AsReadOnly(), readonlyErrorsByPack);
            }

            return ContentLoadResult.Success(loadedPacks.AsReadOnly());
        }

        /// <summary>
        /// Loads content files for a specific type from declared paths or a conventional subdirectory.
        /// </summary>
        private void LoadContentType(
            string packDirectory,
            PackManifest manifest,
            string contentType,
            List<string>? declaredPaths,
            List<string> errors)
        {
            // Collect YAML files to load
            List<string> yamlFiles;

            if (declaredPaths != null && declaredPaths.Count > 0)
            {
                // Declared paths can be relative file paths or directory names
                yamlFiles = new List<string>();
                foreach (string path in declaredPaths)
                {
                    string fullPath = Path.Combine(packDirectory, path);
                    if (Directory.Exists(fullPath))
                    {
                        yamlFiles.AddRange(Directory.GetFiles(fullPath, "*.yaml"));
                        yamlFiles.AddRange(Directory.GetFiles(fullPath, "*.yml"));
                    }
                    else if (File.Exists(fullPath))
                    {
                        yamlFiles.Add(fullPath);
                    }
                    else
                    {
                        // Try as a relative path with yaml extension
                        string withExt = fullPath.EndsWith(".yaml") || fullPath.EndsWith(".yml")
                            ? fullPath
                            : fullPath + ".yaml";
                        if (File.Exists(withExt))
                            yamlFiles.Add(withExt);
                    }
                }
            }
            else
            {
                // Scan conventional subdirectory
                string subDir = Path.Combine(packDirectory, contentType);
                if (!Directory.Exists(subDir))
                    return;

                yamlFiles = new List<string>(Directory.GetFiles(subDir, "*.yaml"));
                yamlFiles.AddRange(Directory.GetFiles(subDir, "*.yml"));
            }

            foreach (string yamlFile in yamlFiles)
            {
                LoadAndRegisterContent(yamlFile, contentType, manifest, errors);
            }
        }

        /// <summary>
        /// Scans conventional subdirectories (units/, buildings/, factions/, etc.) for content files.
        /// </summary>
        private void ScanConventionalDirectories(string packDirectory, PackManifest manifest, List<string> errors)
        {
            foreach (string contentType in ContentTypeToSchema.Keys)
            {
                LoadContentType(packDirectory, manifest, contentType, null, errors);
            }
        }

        /// <summary>
        /// Reads a YAML file, optionally validates it, deserializes it, and registers it.
        /// </summary>
        private void LoadAndRegisterContent(
            string yamlFilePath,
            string contentType,
            PackManifest manifest,
            List<string> errors)
        {
            string yamlContent;
            try
            {
                yamlContent = File.ReadAllText(yamlFilePath);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to read {yamlFilePath}: {ex.Message}");
                return;
            }

            // Validate against schema if validator is available
            if (_schemaValidator != null && ContentTypeToSchema.TryGetValue(contentType, out string? schemaName))
            {
                try
                {
                    ValidationResult validationResult = _schemaValidator.Validate(schemaName, yamlContent);
                    if (!validationResult.IsValid)
                    {
                        foreach (ValidationError error in validationResult.Errors)
                        {
                            errors.Add($"Validation error in {yamlFilePath} [{error.Path}]: {error.Message}");
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // Log validation failure but continue (schema might not be loaded)
                    _log($"[ContentLoader] Schema validation skipped for {yamlFilePath}: {ex.Message}");
                }
            }

            // Deserialize and register
            try
            {
                RegisterContent(yamlContent, contentType, manifest);
                _log($"[ContentLoader] Registered {contentType} from {Path.GetFileName(yamlFilePath)}");
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to deserialize/register {yamlFilePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Deserializes YAML content to the appropriate model type and registers it.
        /// Handles both single objects and lists of objects.
        /// </summary>
        private void RegisterContent(string yamlContent, string contentType, PackManifest manifest)
        {
            switch (contentType.ToLowerInvariant())
            {
                case "units":
                    var units = TryDeserializeList<UnitDefinition>(yamlContent)
                        ?? new List<UnitDefinition> { _deserializer.Deserialize<UnitDefinition>(yamlContent) };
                    foreach (var unit in units)
                    {
                        _registryManager.Units.Register(unit.Id, unit, RegistrySource.Pack, manifest.Id, manifest.LoadOrder);
                    }
                    break;

                case "buildings":
                    var buildings = TryDeserializeList<BuildingDefinition>(yamlContent)
                        ?? new List<BuildingDefinition> { _deserializer.Deserialize<BuildingDefinition>(yamlContent) };
                    foreach (var building in buildings)
                    {
                        _registryManager.Buildings.Register(building.Id, building, RegistrySource.Pack, manifest.Id, manifest.LoadOrder);
                    }
                    break;

                case "factions":
                    var factions = TryDeserializeList<FactionDefinition>(yamlContent)
                        ?? new List<FactionDefinition> { _deserializer.Deserialize<FactionDefinition>(yamlContent) };
                    foreach (var faction in factions)
                    {
                        _registryManager.Factions.Register(faction.Faction.Id, faction, RegistrySource.Pack, manifest.Id, manifest.LoadOrder);
                    }
                    break;

                case "weapons":
                    var weapons = TryDeserializeList<WeaponDefinition>(yamlContent)
                        ?? new List<WeaponDefinition> { _deserializer.Deserialize<WeaponDefinition>(yamlContent) };
                    foreach (var weapon in weapons)
                    {
                        _registryManager.Weapons.Register(weapon.Id, weapon, RegistrySource.Pack, manifest.Id, manifest.LoadOrder);
                    }
                    break;

                case "projectiles":
                    var projectiles = TryDeserializeList<ProjectileDefinition>(yamlContent)
                        ?? new List<ProjectileDefinition> { _deserializer.Deserialize<ProjectileDefinition>(yamlContent) };
                    foreach (var projectile in projectiles)
                    {
                        _registryManager.Projectiles.Register(projectile.Id, projectile, RegistrySource.Pack, manifest.Id, manifest.LoadOrder);
                    }
                    break;

                case "doctrines":
                    var doctrines = TryDeserializeList<DoctrineDefinition>(yamlContent)
                        ?? new List<DoctrineDefinition> { _deserializer.Deserialize<DoctrineDefinition>(yamlContent) };
                    foreach (var doctrine in doctrines)
                    {
                        _registryManager.Doctrines.Register(doctrine.Id, doctrine, RegistrySource.Pack, manifest.Id, manifest.LoadOrder);
                    }
                    break;

                case "stats":
                    var statOverrides = TryDeserializeList<StatOverrideDefinition>(yamlContent)
                        ?? new List<StatOverrideDefinition> { _deserializer.Deserialize<StatOverrideDefinition>(yamlContent) };
                    _loadedOverrides.AddRange(statOverrides);
                    break;

                default:
                    _log($"[ContentLoader] Unknown content type '{contentType}', skipping.");
                    break;
            }
        }

        /// <summary>
        /// Attempts to deserialize YAML content as a list of items.
        /// Returns null if the content is not a list.
        /// </summary>
        private List<T>? TryDeserializeList<T>(string yamlContent) where T : class
        {
            try
            {
                // Try to deserialize as a list
                return _deserializer.Deserialize<List<T>>(yamlContent);
            }
            catch
            {
                // If deserialization as list fails, return null to fall back to single object
                return null;
            }
        }
    }
}
