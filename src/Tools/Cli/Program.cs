#nullable enable
using System.CommandLine;
using DINOForge.Tools.Cli.Assetctl;
using DINOForge.Tools.Cli.Assetctl.Sketchfab;
using DINOForge.Tools.Cli.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Build configuration from appsettings.json and environment variables
IConfigurationBuilder configBuilder = new ConfigurationBuilder()
    .SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".")
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

IConfiguration configuration = configBuilder.Build();

// Configure dependency injection container
IServiceCollection services = new ServiceCollection();

// Register configuration
services.Configure<SketchfabConfiguration>(configuration.GetSection("Sketchfab"));
services.Configure<AssetPipelineConfiguration>(configuration.GetSection("AssetPipeline"));

// Register logging first (before other services)
services.AddLogging(builder =>
{
    builder.AddConsole();
    var logLevel = configuration["Logging:LogLevel:Default"] ?? "Information";
    builder.SetMinimumLevel(Enum.Parse<LogLevel>(logLevel));
});

// Register HTTP client factory for Sketchfab with timeout configuration
services.AddHttpClient<SketchfabClient>()
    .ConfigureHttpClient((sp, client) =>
    {
        var sketchfabConfig = configuration.GetSection("Sketchfab").Get<SketchfabConfiguration>() ?? new SketchfabConfiguration();
        client.Timeout = TimeSpan.FromSeconds(sketchfabConfig.HttpTimeoutSeconds);
        client.DefaultRequestHeaders.Add("User-Agent", "DINOForge-AssetCLI/1.0");
    });

// Register Sketchfab adapter (ISketchfabAdapter -> SketchfabAdapter)
services.AddScoped<ISketchfabAdapter, SketchfabAdapter>();

// Register asset downloader
services.AddScoped<AssetDownloader>();

IServiceProvider serviceProvider = services.BuildServiceProvider();

// Log DI registration on startup
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("DINOForge CLI initialized with dependency injection");
logger.LogInformation("Registered services: SketchfabClient, ISketchfabAdapter, AssetDownloader");

// Validate Sketchfab API token at startup
var sketchfabToken = Environment.GetEnvironmentVariable("SKETCHFAB_API_TOKEN");
if (string.IsNullOrWhiteSpace(sketchfabToken))
{
    // Warning only - allow CLI to run but error if Sketchfab commands are invoked
    logger.LogWarning(
        "SKETCHFAB_API_TOKEN environment variable not set. " +
        "Sketchfab commands will fail. See docs/SKETCHFAB_QUICK_START.md for setup instructions.");
}
else
{
    logger.LogInformation("SKETCHFAB_API_TOKEN is set ({TokenLength} chars)", sketchfabToken.Length);
}

RootCommand rootCommand = new("DINOForge CLI - command-line interface for the DINO mod platform");

rootCommand.Add(StatusCommand.Create());
rootCommand.Add(InstallCommand.Create());
rootCommand.Add(QueryCommand.Create());
rootCommand.Add(DumpCommand.Create());
rootCommand.Add(OverrideCommand.Create());
rootCommand.Add(ResourcesCommand.Create());
rootCommand.Add(VerifyPackCommand.Create());
rootCommand.Add(ReloadCommand.Create());
rootCommand.Add(ScreenshotCommand.Create());
rootCommand.Add(ComponentMapCommand.Create());
rootCommand.Add(WatchCommand.Create());
rootCommand.Add(AssetctlCommand.Create(serviceProvider));

ParseResult parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
