using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Remora.Commands.Extensions;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Extensions;
using Remora.Rest.Core;

using Serilog;

using Uwuify.DiscordBot.WorkerService.Commands;
using Uwuify.DiscordBot.WorkerService.Models;


var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", true)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration
    .GetSection(nameof(DiscordSettings))
    .Get<DiscordSettings>();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Seq(
        serverUrl: settings.MetricsUri,
        apiKey: settings.MetricsToken)
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var shardIds = Enumerable.Range(0, settings.Shards);
var createdShards = 0;

var responderTypes = typeof(Program).Assembly
    .GetExportedTypes()
    .Where(t => t.IsResponder());

try
{
    await Parallel.ForEachAsync(shardIds, async (shardId, ct) =>
    {
        var client = CreateHost(args, configuration, shardId, settings);
        await StartupActions(client); var delay = TimeSpan.FromSeconds(10 * createdShards++);
        await Task.Delay(delay, ct); // Internal sharding must be delayed by at least 5s
        await client.RunAsync(ct);
    });
}
catch (Exception e)
{
    Log.Fatal(e, "Hosted service crashed!");
    Log.CloseAndFlush();
}

IHost CreateHost(string[] args, IConfigurationRoot configuration, int shardId,
    DiscordSettings settings) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog(Log.Logger)
        .AddDiscordService(_ => settings.Token)
        .ConfigureServices(serviceCollection =>
        {
            // Configuration
            serviceCollection
                .AddSingleton(configuration)
                .AddSingleton(configuration
                    .GetSection(nameof(DiscordSettings))
                    .Get<DiscordSettings>());

            // Discord
            serviceCollection
                .AddDiscordCommands(true)
                .AddCommandTree()
                .WithCommandGroup<UwuifyCommands>()
                .WithCommandGroup<MiscCommands>()
                .Finish()
                .AddTransient<IOptions<DiscordGatewayClientOptions>>(_ =>
                    new OptionsWrapper<DiscordGatewayClientOptions>(new DiscordGatewayClientOptions
                    {
                        ShardIdentification = new ShardIdentification(
                            shardId,
                            settings.Shards)
                    }));

            foreach (var responderType in responderTypes)
            {
                serviceCollection.AddResponder(responderType);
            }
        })
        .Build();

void ValidateSlashCommandSupport(SlashService slashService)
{
    var checkSlashSupport = slashService.SupportsSlashCommands();

    if (!checkSlashSupport.IsSuccess)
    {
        Log.Logger.Warning(
            "The registered commands of the bot don't support slash commands: {Reason}",
            checkSlashSupport.Error!.Message);
    }
}

async Task UpdateDebugSlashCommands(DiscordSettings discordSettings, SlashService slashService)
{
    var debugServerString = discordSettings.DebugServerId;

    if (!debugServerString.HasValue)
    {
        Log.Logger.Warning("Failed to parse debug server from configuration!");
        return;
    }

    var updateSlash = await slashService.UpdateSlashCommandsAsync(new Snowflake(debugServerString.Value));
    if (!updateSlash.IsSuccess)
    {
        Log.Logger.Warning(
            "Failed to update slash commands: {Reason}",
            updateSlash.Error!.Message);
    }
}

async Task StartupActions(IHost client)
{
    ValidateSlashCommandSupport(client.Services.GetRequiredService<SlashService>());

#if DEBUG
    await UpdateDebugSlashCommands(
        client.Services.GetRequiredService<DiscordSettings>(),
        client.Services.GetRequiredService<SlashService>());
#endif
}