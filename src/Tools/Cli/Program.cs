#nullable enable
using System.CommandLine;
using DINOForge.Tools.Cli.Commands;

RootCommand rootCommand = new("DINOForge CLI - command-line interface for the DINO mod platform");

rootCommand.Add(StatusCommand.Create());
rootCommand.Add(QueryCommand.Create());
rootCommand.Add(DumpCommand.Create());
rootCommand.Add(OverrideCommand.Create());
rootCommand.Add(ResourcesCommand.Create());
rootCommand.Add(VerifyPackCommand.Create());
rootCommand.Add(ReloadCommand.Create());
rootCommand.Add(ScreenshotCommand.Create());
rootCommand.Add(ComponentMapCommand.Create());
rootCommand.Add(WatchCommand.Create());

ParseResult parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
