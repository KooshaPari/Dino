using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using DINOForge.SDK;
using DINOForge.SDK.Assets;

namespace DINOForge.Tools.PackCompiler
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var packPathArg = new Argument<string>("pack-path", "Path to the pack directory");
            var outputOption = new Option<string?>(new[] { "--output", "-o" }, "Output directory for the bundled pack");

            // Validate command
            var validateCommand = new Command("validate", "Validate a pack directory")
            {
                packPathArg
            };
            validateCommand.SetHandler((packPath) => ValidatePack(packPath), packPathArg);

            // Build command
            var buildCommand = new Command("build", "Validate and bundle a pack directory")
            {
                packPathArg,
                outputOption
            };
            buildCommand.SetHandler((packPath, outputDir) => BuildPack(packPath, outputDir), packPathArg, outputOption);

            // Assets command group
            var assetsCommand = new Command("assets", "Inspect and validate Unity asset bundles");

            var gameDirArg = new Argument<string>("game-dir", "Game installation directory");
            var assetsListCommand = new Command("list", "List all game asset bundles")
            {
                gameDirArg
            };
            assetsListCommand.SetHandler((gameDir) => AssetsListBundles(gameDir), gameDirArg);

            var bundlePathArg = new Argument<string>("bundle-path", "Path to a .bundle file");
            var assetsInspectCommand = new Command("inspect", "List assets in a bundle")
            {
                bundlePathArg
            };
            assetsInspectCommand.SetHandler((bundlePath) => AssetsInspect(bundlePath), bundlePathArg);

            var modBundlePathArg = new Argument<string>("mod-bundle-path", "Path to a mod .bundle file");
            var assetsValidateCommand = new Command("validate", "Validate a mod asset bundle")
            {
                modBundlePathArg
            };
            assetsValidateCommand.SetHandler((modBundlePath) => AssetsValidate(modBundlePath), modBundlePathArg);

            assetsCommand.AddCommand(assetsListCommand);
            assetsCommand.AddCommand(assetsInspectCommand);
            assetsCommand.AddCommand(assetsValidateCommand);

            var rootCommand = new RootCommand("DINOForge PackCompiler - Validate and bundle content packs")
            {
                validateCommand,
                buildCommand,
                assetsCommand
            };

            return await rootCommand.InvokeAsync(args);
        }

        private static void ValidatePack(string packPath)
        {
            try
            {
                AnsiConsole.MarkupLine("[bold blue]PackCompiler Validate[/]");
                AnsiConsole.MarkupLine($"Pack Path: {packPath}");
                AnsiConsole.WriteLine();

                // Check if directory exists
                if (!Directory.Exists(packPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Error:[/] Pack directory not found");
                    Environment.Exit(1);
                }

                // Check for pack.yaml
                string manifestPath = Path.Combine(packPath, "pack.yaml");
                if (!File.Exists(manifestPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Error:[/] pack.yaml not found in directory");
                    Environment.Exit(1);
                }

                // Load and validate manifest
                AnsiConsole.MarkupLine("[yellow]Loading manifest...[/]");
                var loader = new PackLoader();
                var manifest = loader.LoadFromFile(manifestPath);

                // Display manifest information
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

                // Scan for content files
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

                // Check if directory exists
                if (!Directory.Exists(packPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Error:[/] Pack directory not found");
                    Environment.Exit(1);
                }

                // Check for pack.yaml
                string manifestPath = Path.Combine(packPath, "pack.yaml");
                if (!File.Exists(manifestPath))
                {
                    AnsiConsole.MarkupLine("[bold red]Error:[/] pack.yaml not found in directory");
                    Environment.Exit(1);
                }

                // Validate manifest
                AnsiConsole.MarkupLine("[yellow]Validating manifest...[/]");
                var loader = new PackLoader();
                var manifest = loader.LoadFromFile(manifestPath);
                AnsiConsole.MarkupLine($"[green]✓[/] Manifest valid: {manifest.Name} v{manifest.Version}");
                AnsiConsole.WriteLine();

                // Determine output path
                string finalOutputDir = outputDir ?? Path.Combine(Directory.GetCurrentDirectory(), $"{manifest.Id}-{manifest.Version}");

                // Create output directory
                if (Directory.Exists(finalOutputDir))
                {
                    AnsiConsole.MarkupLine($"[yellow]Clearing existing output directory...[/]");
                    Directory.Delete(finalOutputDir, true);
                }

                AnsiConsole.MarkupLine($"[yellow]Copying pack to output directory...[/]");
                CopyDirectory(packPath, finalOutputDir);

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

                // Unity version
                AnsiConsole.MarkupLine($"Unity Version: [bold]{Markup.Escape(result.UnityVersion)}[/]");
                AnsiConsole.MarkupLine($"Expected: [bold]{AssetService.ExpectedUnityVersion}.x[/]");
                AnsiConsole.WriteLine();

                // Errors
                if (result.Errors.Count > 0)
                {
                    AnsiConsole.MarkupLine("[bold red]Validation Errors:[/]");
                    foreach (string error in result.Errors)
                    {
                        AnsiConsole.MarkupLine($"  [red]x[/] {Markup.Escape(error)}");
                    }
                    AnsiConsole.WriteLine();
                }

                // Assets summary
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
    }
}
