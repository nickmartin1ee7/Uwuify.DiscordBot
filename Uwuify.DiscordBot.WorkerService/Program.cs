using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Hosting.Extensions;
using Remora.Rest.Core;
using Serilog;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
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

        var serilogApiKey = configuration.GetSection("Serilog")
            .GetValue<string>("ApiKey");

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Seq("http://seq:80",
                apiKey: serilogApiKey)
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        var token = configuration
            .GetSection(nameof(DiscordSettings))
            .GetValue<string>("Token");

        var shardCount = configuration.GetValue<int>("ShardCount");

        var (shardResponse, shardId) = await TryGetShardIdAsync();

        var host = Host.CreateDefaultBuilder(args)
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
                    .AddCommandGroup<UserCommands>()
                    .AddTransient<IOptions<DiscordGatewayClientOptions>>(_ => shardResponse.IsSuccessStatusCode
                        ? new OptionsWrapper<DiscordGatewayClientOptions>(new DiscordGatewayClientOptions
                        {
                            ShardIdentification = new ShardIdentification(
                                shardId,
                                shardCount)
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
            .AddDiscordService(_ => token)
            .Build();

        ValidateSlashCommandSupport(host.Services.GetRequiredService<SlashService>());
#if DEBUG
        await UpdateDebugSlashCommands(
            host.Services.GetRequiredService<DiscordSettings>(),
            host.Services.GetRequiredService<SlashService>());
#endif

        try
        {
            await host.RunAsync();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Hosted service crashed!");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static async Task<(HttpResponseMessage shardResponse, int shardId)> TryGetShardIdAsync()
    {
        using var shardHttpClient = new HttpClient();
        var shardResponse = await shardHttpClient.GetAsync("http://uwuify.shardmanager.webapi/requestId");
        _ = int.TryParse(await shardResponse.Content.ReadAsStringAsync(), out var shardId);
        return (shardResponse, shardId);
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
