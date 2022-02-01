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
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.ShardManager.Models;
using Uwuify.DiscordBot.WorkerService.Commands;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService;
public static class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var settings = configuration
            .GetSection(nameof(DiscordSettings))
            .Get<DiscordSettings>();

        var loggerConfig = configuration.GetSection("LoggerConfig");

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Seq(
                serverUrl: loggerConfig.GetValue<string>("ServerUri"),
                apiKey: loggerConfig.GetValue<string>("ApiKey"))
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        var (shardResponse, shardGroup) = await DecideShardingAsync(settings.ShardManagerUri);
        var shouldShard = shardResponse.IsSuccessStatusCode;

        var shardClients = shardGroup.ShardIds
            .Select(shardId => CreateHost(args, configuration, shouldShard, shardId, shardGroup, settings))
            .ToList();

        try
        {
            int runningClients = 0;
            await Task.WhenAll(shardClients.Select(async shardClient =>
            {
                ValidateSlashCommandSupport(shardClient.Services.GetRequiredService<SlashService>());
#if DEBUG
                await UpdateDebugSlashCommands(
                    shardClient.Services.GetRequiredService<DiscordSettings>(),
                    shardClient.Services.GetRequiredService<SlashService>());
#endif
                var delay = TimeSpan.FromSeconds(10 * runningClients++);
                await Task.Delay(delay); // Internal sharding must be delayed by at least 5s
                await shardClient.RunAsync();
            }));
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Hosted service crashed!");
        }
        finally
        {
            Log.Logger.Information("Client exiting. Giving up shard group: {shardGroup}.", shardGroup);
            Log.CloseAndFlush();
            using var internalShardHttpClient = new HttpClient();
            _ = await internalShardHttpClient.GetAsync(
                    $"{settings.ShardManagerUri}/unassignShardGroup?groupId={shardGroup.GroupId}");
        }
    }

    private static IHost CreateHost(string[] args, IConfigurationRoot configuration, bool shouldShard, int shardId, ShardGroup shardGroup, DiscordSettings settings) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog(Log.Logger)
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
                    .AddCommandGroup<UwuifyCommands>()
                    .AddCommandGroup<MiscCommands>()
                    .AddTransient<IOptions<DiscordGatewayClientOptions>>(_ => shouldShard
                        ? new OptionsWrapper<DiscordGatewayClientOptions>(new DiscordGatewayClientOptions
                        {
                            ShardIdentification = new ShardIdentification(
                                shardId,
                                shardGroup.MaxShards)
                        })
                        : new OptionsWrapper<DiscordGatewayClientOptions>(new DiscordGatewayClientOptions()));

                var responderTypes = typeof(Program).Assembly
                    .GetExportedTypes()
                    .Where(t => t.IsResponder());

                foreach (var responderType in responderTypes)
                {
                    serviceCollection.AddResponder(responderType);
                }
            })
            .AddDiscordService(_ => settings.Token)
            .Build();

    private static async Task<(HttpResponseMessage shardResponse, ShardGroup shardGroup)> DecideShardingAsync(string shardManagerUri, int attempts = 5)
    {
        using var shardHttpClient = new HttpClient();
        HttpResponseMessage shardResponse = null;

        for (int i = 0; i < attempts; i++)
        {
            try
            {
                shardResponse =
                    await shardHttpClient.GetAsync(
                        $"{shardManagerUri}/requestShardGroup");

                break;
            }
            catch (HttpRequestException e)
            {
                Log.Logger.Warning(e, "Failed to connect to Shard Manager. Attempt {i} of {attempts}.", i + 1, attempts);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        if (shardResponse is null)
        {
            Log.Logger.Error("Failed to connect to Shard Manager. Out of Attempts.");
            Environment.Exit(-1);
        }

        switch (shardResponse.StatusCode)
        {
            case HttpStatusCode.Conflict:
                Log.Logger.Warning("No more shards available for client");
                Environment.Exit(0);
                break;
            default:
                var content = await shardResponse.Content.ReadAsStringAsync();
                var shardGroup = JsonSerializer.Deserialize<ShardGroup>(content, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                shardResponse.Dispose();
                return (shardResponse, shardGroup);
        }
        
        return default;
    }

    private static void ValidateSlashCommandSupport(SlashService slashService)
    {
        var checkSlashSupport = slashService.SupportsSlashCommands();
        if (!checkSlashSupport.IsSuccess)
        {
            Log.Logger.Warning(
                "The registered commands of the bot don't support slash commands: {Reason}",
                checkSlashSupport.Error!.Message);
        }
    }

    private static async Task UpdateDebugSlashCommands(DiscordSettings discordSettings, SlashService slashService)
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
}
