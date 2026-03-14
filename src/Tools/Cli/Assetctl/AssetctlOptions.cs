#nullable enable
using System.CommandLine;

namespace DINOForge.Tools.Cli.Assetctl
{
    /// <summary>
    /// Shared System.CommandLine option factories for assetctl subcommands.
    /// </summary>
    public static class AssetctlOptions
    {
        /// <summary>Returns the --pipeline-root option (path to the pack asset pipeline root).</summary>
        public static Option<string> PipelineRootOption()
        {
            Option<string> opt = new("--pipeline-root", "Path to the pack asset pipeline root directory");
            opt.SetDefaultValue(".");
            return opt;
        }

        /// <summary>Returns the --format option (json | table).</summary>
        public static Option<string> FormatOption()
        {
            Option<string> opt = new("--format", "Output format: 'json' for machine-readable, 'table' for human-readable (default)");
            opt.SetDefaultValue("table");
            return opt;
        }
    }
}
