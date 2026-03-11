using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Spectre.Console;
using DINOForge.SDK;
using DINOForge.SDK.Assets;
using DINOForge.SDK.Models;
using DINOForge.SDK.Validation;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DINOForge.Tools.PackCompiler
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var packPathArg = new Argument<string>("pack-path") { Description = "Path to the pack directory" };
            var outputOption = new Option<string?>("--output", "-o") { Description = "Output directory for the bundled pack" };

            // Validate command
            var validateCommand = new Command("validate") { Description = "Validate a pack directory" };
            validateCommand.Arguments.Add(packPathArg);
            validateCommand.SetAction(parseResult =>
            {
                string packPath = parseResult.GetValue(packPathArg)!;
                ValidatePack(packPath);
            });

            // Build command
            var buildPackPathArg = new Argument<string>("pack-path") { Description = "Path to the pack directory" };
            var buildCommand = new Command("build") { Description = "Validate and bundle a pack directory" };
            buildCommand.Arguments.Add(buildPackPathArg);
            buildCommand.Options.Add(outputOption);
            buildCommand.SetAction(parseResult =>
            {
                string packPath = parseResult.GetValue(buildPackPathArg)!;
                string? outputDir = parseResult.GetValue(outputOption);
                BuildPack(packPath, outputDir);
            });

            // Assets command group
            var assetsCommand = new Command("assets") { Description = "Inspect and validate Unity asset bundles" };

            var gameDirArg = new Argument<string>("game-dir") { Description = "Game installation directory" };
            var assetsListCommand = new Command("list") { Description = "List all game asset bundles" };
            assetsListCommand.Arguments.Add(gameDirArg);
            assetsListCommand.SetAction(parseResult =>
            {
                string gameDir = parseResult.GetValue(gameDirArg)!;
                AssetsListBundles(gameDir);
            });

            var bundlePathArg = new Argument<string>("bundle-path") { Description = "Path to a .bundle file" };
            var assetsInspectCommand = new Command("inspect") { Description = "List assets in a bundle" };
            assetsInspectCommand.Arguments.Add(bundlePathArg);
            assetsInspectCommand.SetAction(parseResult =>
            {
                string bundlePath = parseResult.GetValue(bundlePathArg)!;
                AssetsInspect(bundlePath);
            });

            var modBundlePathArg = new Argument<string>("mod-bundle-path") { Description = "Path to a mod .bundle file" };
            var assetsValidateCommand = new Command("validate") { Description = "Validate a mod asset bundle" };
            assetsValidateCommand.Arguments.Add(modBundlePathArg);
            assetsValidateCommand.SetAction(parseResult =>
            {
                string modBundlePath = parseResult.GetValue(modBundlePathArg)!;
                AssetsValidate(modBundlePath);
            });

            assetsCommand.Subcommands.Add(assetsListCommand);
            assetsCommand.Subcommands.Add(assetsInspectCommand);
            assetsCommand.Subcommands.Add(assetsValidateCommand);

            // Thunderstore command
            var thunderstorePackDirArg = new Argument<string>("pack-path") { Description = "Path to the pack directory containing pack.yaml" };
            var authorOption = new Option<string?>("--author") { Description = "Thunderstore author name (default: KooshaPari)" };
            var thunderstoreOutputOption = new Option<string?>("--output", "-o") { Description = "Output directory (defaults to pack-path)" };
            var thunderstoreCommand = new Command("thunderstore") { Description = "Generate Thunderstore-compatible manifest.json from a DINOForge pack" };
            thunderstoreCommand.Arguments.Add(thunderstorePackDirArg);
            thunderstoreCommand.Options.Add(authorOption);
            thunderstoreCommand.Options.Add(thunderstoreOutputOption);
            thunderstoreCommand.SetAction(parseResult =>
            {
                string packPath = parseResult.GetValue(thunderstorePackDirArg)!;
                string author = parseResult.GetValue(authorOption) ?? "KooshaPari";
                string? outputDir = parseResult.GetValue(thunderstoreOutputOption);
                GenerateThunderstoreManifest(packPath, author, outputDir ?? "");
            });

            // Validate total conversion command
            var validateTcCommand = new Command("validate-tc") { Description = "Validate a total conversion pack manifest" };
            var tcManifestArg = new Argument<string>("manifest") { Description = "Path to total-conversion YAML manifest" };
            validateTcCommand.Arguments.Add(tcManifestArg);
            validateTcCommand.SetAction(parseResult =>
            {
                string manifestPath = parseResult.GetValue(tcManifestArg)!;
                ValidateTotalConversion(manifestPath);
            });

            // Pack subcommand - polyrepo pack management
            var packCommand = new Command("pack") { Description = "Manage pack repositories and submodules" };

            // pack add - add a pack repo as a git submodule
            var packAddCommand = new Command("add") { Description = "Add a pack repository as a git submodule" };
            var repoUrlArg = new Argument<string>("repo-url") { Description = "Git repository URL of the pack" };
            var packPathOpt = new Option<string?>("--path") { Description = "Local path for the submodule (defaults to packs/<repo-name>)" };
            packAddCommand.Arguments.Add(repoUrlArg);
            packAddCommand.Options.Add(packPathOpt);
            packAddCommand.SetAction(parseResult =>
            {
                string repoUrl = parseResult.GetValue(repoUrlArg)!;
                string? path = parseResult.GetValue(packPathOpt);
                PackAdd(repoUrl, path ?? "");
            });

            // pack list - list installed pack submodules
            var packListCommand = new Command("list") { Description = "List installed pack submodules" };
            packListCommand.SetAction(_ => PackList());

            // pack update - update all pack submodules
            var packUpdateCommand = new Command("update") { Description = "Update all pack submodules to latest" };
            packUpdateCommand.SetAction(_ => PackUpdate());

            // pack lock - generate packs.lock file
            var packLockCommand = new Command("lock") { Description = "Generate packs.lock for reproducible builds" };
            packLockCommand.SetAction(_ => PackLock());

            packCommand.Subcommands.Add(packAddCommand);
            packCommand.Subcommands.Add(packListCommand);
            packCommand.Subcommands.Add(packUpdateCommand);
            packCommand.Subcommands.Add(packLockCommand);

            var rootCommand = new RootCommand("DINOForge PackCompiler - Validate and bundle content packs");
            rootCommand.Subcommands.Add(validateCommand);
            rootCommand.Subcommands.Add(buildCommand);
            rootCommand.Subcommands.Add(validateTcCommand);
            rootCommand.Subcommands.Add(thunderstoreCommand);
            rootCommand.Subcommands.Add(assetsCommand);
            rootCommand.Subcommands.Add(packCommand);

            ParseResult parseResultObj = rootCommand.Parse(args);
            return await parseResultObj.InvokeAsync();
        }

        private static void ValidatePack(string packPath)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]PackCompiler Validate[/]");
                AnsiConsole.MarkupLine($"Pack Path: {packPath}");
                AnsiConsole.WriteLine();

                if (!Directory.Exists(packPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Error:[/] Pack directory not found");
                    Environment.Exit(1);
                }

                string manifestPath = Path.Combine(packPath, "pack.yaml");
                if (!File.Exists(manifestPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Error:[/] pack.yaml not found in directory");
                    Environment.Exit(1);
                }

                AnsiConsole.MarkupLine("[yellow]Loading manifest...[/]");
                var loader = new PackLoader();
                var manifest = loader.LoadFromFile(manifestPath);

                AnsiConsole.MarkupLine("[bold]Manifest Fields:[/]");
                var table = new Table();
                table.AddColumn("Field");
                table.AddColumn("Value");
                table.AddRow("ID", manifest.Id);
                table.AddRow("Name", manifest.Name);
                table.AddRow("Version", manifest.Version);
                table.AddRow("Author", manifest.Author ?? "[dim]<not set>[/]");
                table.AddRow("Type", manifest.Type);
                table.AddRow("Description", manifest.Description ?? "[dim]<not set>[/]");
                table.AddRow("Framework Version", manifest.FrameworkVersion);
                table.AddRow("Game Version", manifest.GameVersion ?? "[dim]<not set>[/]");
                table.AddRow("Load Order", manifest.LoadOrder.ToString());

                if (manifest.DependsOn.Count > 0)
                    table.AddRow("Depends On", string.Join(", ", manifest.DependsOn));

                if (manifest.ConflictsWith.Count > 0)
                    table.AddRow("Conflicts With", string.Join(", ", manifest.ConflictsWith));

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();

                AnsiConsole.MarkupLine("[bold]Content Files:[/]");
                var contentTable = new Table();
                contentTable.AddColumn("Type");
                contentTable.AddColumn("Count");

                var contentDirs = new[] { "factions", "units", "buildings", "weapons", "doctrines", "audio", "visuals", "localization", "wave_templates", "tech_nodes", "scenarios" };
                var foundContent = false;

                foreach (var dir in contentDirs)
                {
                    string dirPath = Path.Combine(packPath, dir);
                    if (Directory.Exists(dirPath))
                    {
                        var files = Directory.GetFiles(dirPath);
                        if (files.Length > 0)
                        {
                            contentTable.AddRow(dir, files.Length.ToString());
                            foundContent = true;
                        }
                    }
                }

                if (foundContent)
                {
                    AnsiConsole.Write(contentTable);
                }
                else
                {
                    AnsiConsole.MarkupLine("[dim]No content files found[/]");
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold green]Validation successful![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Validation failed:[/] {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static void BuildPack(string packPath, string? outputDir)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]PackCompiler Build[/]");
                AnsiConsole.MarkupLine($"Pack Path: {packPath}");
                if (!string.IsNullOrEmpty(outputDir))
                    AnsiConsole.MarkupLine($"Output Directory: {outputDir}");
                AnsiConsole.WriteLine();

                if (!Directory.Exists(packPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Error:[/] Pack directory not found");
                    Environment.Exit(1);
                }

                string manifestPath = Path.Combine(packPath, "pack.yaml");
                if (!File.Exists(manifestPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Error:[/] pack.yaml not found in directory");
                    Environment.Exit(1);
                }

                AnsiConsole.MarkupLine("[yellow]Validating manifest...[/]");
                var loader = new PackLoader();
                var manifest = loader.LoadFromFile(manifestPath);
                AnsiConsole.MarkupLine($"[green]v[/] Manifest valid: {manifest.Name} v{manifest.Version}");
                AnsiConsole.WriteLine();

                string finalOutputDir = outputDir ?? Path.Combine(Directory.GetCurrentDirectory(), $"{manifest.Id}-{manifest.Version}");

                if (Directory.Exists(finalOutputDir))
                {
                    AnsiConsole.MarkupLine($"[yellow]Clearing existing output directory...[/]");
                    Directory.Delete(finalOutputDir, true);
                }

                AnsiConsole.MarkupLine($"[yellow]Copying pack to output directory...[/]");
                CopyDirectory(packPath, finalOutputDir);

                AnsiConsole.MarkupLine("[yellow]Generating Thunderstore manifest...[/]");
                GenerateThunderstoreManifest(packPath, "KooshaPari", finalOutputDir);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold green]Build successful![/]");
                AnsiConsole.MarkupLine($"Output: {finalOutputDir}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Build failed:[/] {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static void GenerateThunderstoreManifest(string packPath, string author, string outputDir)
        {
            try
            {
                string manifestPath = Path.Combine(packPath, "pack.yaml");
                if (!File.Exists(manifestPath))
                {
                    AnsiConsole.MarkupLine($"[bold red]ERROR:[/] No pack.yaml found in {packPath}");
                    Environment.Exit(1);
                    return;
                }

                var loader = new PackLoader();
                var manifest = loader.LoadFromFile(manifestPath);

                // Build Thunderstore package name: "Author-PackId" (sanitized to alphanumeric + dash)
                string safeName = System.Text.RegularExpressions.Regex.Replace(
                    manifest.Id, @"[^a-zA-Z0-9_]", "-");
                string tsName = $"{author}-{safeName}";

                // Map DINOForge depends_on to Thunderstore format
                // Always include BepInEx as base dependency
                var dependencies = new List<string> { "BepInEx-BepInExPack-5.4.2100" };
                if (manifest.DependsOn != null && manifest.DependsOn.Count > 0)
                {
                    foreach (string dep in manifest.DependsOn)
                    {
                        // Convert DINOForge dep ID to Thunderstore format
                        string safeDep = System.Text.RegularExpressions.Regex.Replace(dep, @"[^a-zA-Z0-9_]", "-");
                        dependencies.Add($"{author}-{safeDep}-1.0.0");
                    }
                }

                // Truncate description to 250 chars (Thunderstore limit)
                string description = manifest.Description ?? $"DINOForge pack: {manifest.Name}";
                if (description.Length > 250)
                    description = description[..247] + "...";

                var tsManifest = new
                {
                    name = tsName,
                    version_number = manifest.Version,
                    website_url = "https://github.com/KooshaPari/Dino",
                    description = description,
                    dependencies = dependencies
                };

                string finalOutputDir = string.IsNullOrEmpty(outputDir) ? packPath : outputDir;
                Directory.CreateDirectory(finalOutputDir);
                string outPath = Path.Combine(finalOutputDir, "thunderstore.manifest.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                string json = JsonSerializer.Serialize(tsManifest, options);
                File.WriteAllText(outPath, json);

                AnsiConsole.MarkupLine($"[green]✓[/] Thunderstore manifest written to: [bold]{outPath}[/]");
                AnsiConsole.MarkupLine($"  Package: [bold]{tsManifest.name}[/] v{tsManifest.version_number}");
                AnsiConsole.MarkupLine($"  Dependencies: [dim]{dependencies.Count}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Thunderstore generation failed:[/] {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static void AssetsListBundles(string gameDir)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]Asset Bundle List[/]");
                AnsiConsole.MarkupLine($"Game Directory: {gameDir}");
                AnsiConsole.WriteLine();

                using var service = new AssetService(gameDir);
                var bundles = service.ListBundles();

                if (bundles.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No bundles found. Check that the game directory is correct.[/]");
                    return;
                }

                var table = new Table();
                table.AddColumn("Bundle");
                table.AddColumn(new TableColumn("Size").RightAligned());
                table.AddColumn(new TableColumn("Assets").RightAligned());

                foreach (BundleInfo bundle in bundles)
                {
                    string sizeStr = FormatBytes(bundle.SizeBytes);
                    table.AddRow(
                        Markup.Escape(bundle.Name),
                        sizeStr,
                        bundle.AssetCount.ToString());
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]Total: {bundles.Count} bundle(s)[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Error:[/] {Markup.Escape(ex.Message)}");
                Environment.Exit(1);
            }
        }

        private static void AssetsInspect(string bundlePath)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]Asset Bundle Inspection[/]");
                AnsiConsole.MarkupLine($"Bundle: {Markup.Escape(bundlePath)}");
                AnsiConsole.WriteLine();

                using var service = new AssetService(Path.GetDirectoryName(bundlePath) ?? ".");
                var assets = service.ListAssets(bundlePath);

                if (assets.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No assets found in bundle.[/]");
                    return;
                }

                var table = new Table();
                table.AddColumn("Name");
                table.AddColumn("Type");
                table.AddColumn(new TableColumn("PathID").RightAligned());
                table.AddColumn(new TableColumn("Size").RightAligned());

                foreach (AssetInfo asset in assets)
                {
                    table.AddRow(
                        Markup.Escape(asset.Name),
                        Markup.Escape(asset.TypeName),
                        asset.PathId.ToString(),
                        FormatBytes(asset.SizeBytes));
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]Total: {assets.Count} asset(s)[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Error:[/] {Markup.Escape(ex.Message)}");
                Environment.Exit(1);
            }
        }

        private static void AssetsValidate(string modBundlePath)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]Mod Bundle Validation[/]");
                AnsiConsole.MarkupLine($"Bundle: {Markup.Escape(modBundlePath)}");
                AnsiConsole.WriteLine();

                using var service = new AssetService(Path.GetDirectoryName(modBundlePath) ?? ".");
                AssetValidationResult result = service.ValidateModBundle(modBundlePath);

                AnsiConsole.MarkupLine($"Unity Version: [bold]{Markup.Escape(result.UnityVersion)}[/]");
                AnsiConsole.MarkupLine($"Expected: [bold]{AssetService.ExpectedUnityVersion}.x[/]");
                AnsiConsole.WriteLine();

                if (result.Errors.Count > 0)
                {
                    AnsiConsole.MarkupLine("[bold red]Validation Errors:[/]");
                    foreach (string error in result.Errors)
                    {
                        AnsiConsole.MarkupLine($"  [red]x[/] {Markup.Escape(error)}");
                    }
                    AnsiConsole.WriteLine();
                }

                if (result.Assets.Count > 0)
                {
                    var typeCounts = result.Assets
                        .GroupBy(a => a.TypeName)
                        .OrderByDescending(g => g.Count());

                    var table = new Table();
                    table.AddColumn("Asset Type");
                    table.AddColumn(new TableColumn("Count").RightAligned());

                    foreach (var group in typeCounts)
                    {
                        table.AddRow(Markup.Escape(group.Key), group.Count().ToString());
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.WriteLine();
                }

                if (result.IsValid)
                {
                    AnsiConsole.MarkupLine("[bold green]Validation passed![/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[bold red]Validation failed.[/]");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Error:[/] {Markup.Escape(ex.Message)}");
                Environment.Exit(1);
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {suffixes[order]}";
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private static void ValidateTotalConversion(string manifestPath)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]Total Conversion Validator[/]");

                if (!File.Exists(manifestPath))
                {
                    AnsiConsole.MarkupLine($"[red]File not found:[/] {manifestPath}");
                    Environment.Exit(1);
                    return;
                }

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                string yaml = File.ReadAllText(manifestPath);
                var manifest = deserializer.Deserialize<TotalConversionManifest>(yaml);

                if (manifest == null)
                {
                    AnsiConsole.MarkupLine("[red]Failed to parse total conversion manifest[/]");
                    Environment.Exit(1);
                    return;
                }

                var result = TotalConversionValidator.Validate(manifest);
                var unreplaced = TotalConversionValidator.GetUnreplacedFactions(manifest);

                AnsiConsole.MarkupLine($"\n[bold]Pack:[/] {manifest.Name} v{manifest.Version}");
                AnsiConsole.MarkupLine($"[bold]Author:[/] {manifest.Author}");
                AnsiConsole.MarkupLine($"[bold]Theme:[/] {manifest.Theme ?? "[dim]<none>[/]"}");
                AnsiConsole.MarkupLine($"[bold]Singleton:[/] {(manifest.Singleton ? "Yes" : "No")}");

                AnsiConsole.MarkupLine($"\n[bold]Content:[/]");
                AnsiConsole.MarkupLine($"  Factions: {manifest.Factions.Count}");
                AnsiConsole.MarkupLine($"  Asset Replacements (total): {manifest.AssetReplacements.Textures.Count + manifest.AssetReplacements.Audio.Count + manifest.AssetReplacements.Ui.Count}");
                AnsiConsole.MarkupLine($"    - Textures: {manifest.AssetReplacements.Textures.Count}");
                AnsiConsole.MarkupLine($"    - Audio: {manifest.AssetReplacements.Audio.Count}");
                AnsiConsole.MarkupLine($"    - UI: {manifest.AssetReplacements.Ui.Count}");

                AnsiConsole.MarkupLine($"\n[bold]Vanilla Replacements:[/]");
                foreach (var kvp in manifest.ReplacesVanilla)
                {
                    var faction = manifest.Factions.FirstOrDefault(f => f.Id == kvp.Value);
                    string factionName = faction?.Name ?? "[red]<not found>[/]";
                    AnsiConsole.MarkupLine($"  {kvp.Key} → {kvp.Value} ({factionName})");
                }

                if (result.IsValid && unreplaced.Count == 0)
                {
                    AnsiConsole.MarkupLine($"\n[green]✓ Total conversion '{manifest.Name}' is [bold]valid[/][/]");
                }
                else
                {
                    if (result.Errors.Count > 0)
                    {
                        AnsiConsole.MarkupLine("\n[red]Errors:[/]");
                        foreach (string e in result.Errors)
                            AnsiConsole.MarkupLine($"  [red]✗[/] {Markup.Escape(e)}");
                    }

                    if (unreplaced.Count > 0)
                        AnsiConsole.MarkupLine($"\n[yellow]Unreplaced vanilla factions:[/] {string.Join(", ", unreplaced)}");

                    if (result.Errors.Count > 0)
                    {
                        AnsiConsole.MarkupLine("");
                        Environment.Exit(1);
                    }
                }

                if (result.Warnings.Count > 0)
                {
                    AnsiConsole.MarkupLine("\n[yellow]Warnings:[/]");
                    foreach (string w in result.Warnings)
                        AnsiConsole.MarkupLine($"  [yellow]⚠[/] {Markup.Escape(w)}");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold red]Error:[/] {Markup.Escape(ex.Message)}");
                Environment.Exit(1);
            }
        }

        private static void PackAdd(string repoUrl, string path)
        {
            // Extract repo name from URL
            string repoName = repoUrl.TrimEnd('/').Split('/').Last();
            if (repoName.EndsWith(".git")) repoName = repoName[..^4];

            string submodulePath = string.IsNullOrEmpty(path) ? $"packs/{repoName}" : path;

            AnsiConsole.MarkupLine($"[cyan]Adding pack submodule:[/] {repoUrl} → {submodulePath}");

            int result = RunGit($"submodule add {repoUrl} {submodulePath}");
            if (result != 0)
            {
                AnsiConsole.MarkupLine($"[red]Failed to add submodule. Is this a git repo?[/]");
                Environment.Exit(1);
            }

            AnsiConsole.MarkupLine($"[green]✓ Pack added:[/] {submodulePath}");
            AnsiConsole.MarkupLine("[dim]Run 'pack lock' to update packs.lock[/]");
        }

        private static void PackList()
        {
            // Read .gitmodules
            string gitmodulesPath = ".gitmodules";
            if (!File.Exists(gitmodulesPath))
            {
                AnsiConsole.MarkupLine("[yellow]No pack submodules found (.gitmodules not present)[/]");
                return;
            }

            string content = File.ReadAllText(gitmodulesPath);
            var lines = content.Split('\n');

            var table = new Table();
            table.AddColumn("Path");
            table.AddColumn("URL");

            string currentPath = "", currentUrl = "";
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("path = ")) currentPath = trimmed.Substring(7);
                else if (trimmed.StartsWith("url = "))
                {
                    currentUrl = trimmed.Substring(6);
                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        bool isPack = currentPath.StartsWith("packs/");
                        if (isPack) table.AddRow(currentPath, currentUrl);
                        currentPath = "";
                        currentUrl = "";
                    }
                }
            }

            AnsiConsole.Write(table);
        }

        private static void PackUpdate()
        {
            AnsiConsole.MarkupLine("[cyan]Updating all pack submodules...[/]");
            int result = RunGit("submodule update --remote --merge packs/");
            if (result == 0)
                AnsiConsole.MarkupLine("[green]✓ All packs updated[/]");
            else
                AnsiConsole.MarkupLine("[red]Some updates failed[/]");
        }

        private static void PackLock()
        {
            // Read current submodule SHAs
            string gitmodulesPath = ".gitmodules";
            if (!File.Exists(gitmodulesPath))
            {
                AnsiConsole.MarkupLine("[yellow]No submodules found[/]");
                return;
            }

            var lockEntries = new StringBuilder();
            lockEntries.AppendLine("# packs.lock - generated by dinoforge pack lock");
            lockEntries.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            lockEntries.AppendLine();

            // Get submodule status
            var psi = new ProcessStartInfo("git", "submodule status packs/")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi);
            string? status = proc?.StandardOutput.ReadToEnd();
            proc?.WaitForExit();

            if (!string.IsNullOrEmpty(status))
            {
                foreach (string line in status.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string trimmed = line.TrimStart('+', '-', ' ');
                    string[] parts = trimmed.Split(' ', 2);
                    if (parts.Length >= 2)
                        lockEntries.AppendLine($"{parts[1].Split(' ')[0]} {parts[0]}");
                }
            }

            File.WriteAllText("packs.lock", lockEntries.ToString());
            AnsiConsole.MarkupLine("[green]✓ packs.lock generated[/]");
        }

        private static int RunGit(string args)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                UseShellExecute = false
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit();
            return proc?.ExitCode ?? 1;
        }
    }
}
