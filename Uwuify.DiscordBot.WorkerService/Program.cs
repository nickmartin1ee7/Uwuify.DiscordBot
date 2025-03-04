using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using ProfanityFilter.Interfaces;

using Remora.Commands.Extensions;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Extensions;
using Remora.Rest.Core;

using Serilog;

using Uwuify.DiscordBot.Data;
using Uwuify.DiscordBot.WorkerService;
using Uwuify.DiscordBot.WorkerService.Commands;
using Uwuify.DiscordBot.WorkerService.Models;

var host = Host
    .CreateDefaultBuilder()
    .Build();

var configuration = host.Services.GetRequiredService<IConfiguration>();

var settings = configuration
    .GetSection(nameof(DiscordSettings))
    .Get<DiscordSettings>();

ArgumentNullException.ThrowIfNull(settings, nameof(DiscordSettings));

if (string.IsNullOrWhiteSpace(settings.MetricsUri))
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();

}
else
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Seq(
            serverUrl: settings.MetricsUri,
            apiKey: settings.MetricsToken)
        .ReadFrom.Configuration(configuration)
        .CreateLogger();
}

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
        await StartupActions(client);
        var delay = TimeSpan.FromSeconds(10 * createdShards++);
        await Task.Delay(delay, ct); // Internal sharding must be delayed by at least 5s
        await client.RunAsync(ct);
    });
}
catch (Exception e)
{
    Log.Fatal(e, "Hosted service crashed!");
    Log.CloseAndFlush();
}

IHost CreateHost(string[] args, IConfiguration configuration, int shardId,
    DiscordSettings settings) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog(Log.Logger)
        .AddDiscordService(_ => settings.Token)
        .ConfigureServices(serviceCollection =>
        {
            // Configuration
            serviceCollection
                .AddTransient<IProfanityFilter>(_ =>
                    string.IsNullOrWhiteSpace(settings.ProfanityWords)
                    || !settings.ProfanityList.Any()
                    ? new ProfanityFilter.ProfanityFilter()
                    : new ProfanityFilter.ProfanityFilter(settings.ProfanityList))
                .AddSingleton(configuration)
                .AddSingleton(configuration
                    .GetSection(nameof(DiscordSettings))
            .Get<DiscordSettings>());

            serviceCollection.AddDbContext<DataContext>((sp, options) =>
            {
                var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(nameof(DataContext));
                options.UseNpgsql(connectionString: connectionString);
            },
            contextLifetime: ServiceLifetime.Transient);

            serviceCollection.AddSingleton<RateLimitGuardService>();

            serviceCollection.AddSingleton<HttpClient>(sp =>
            {
                var client = sp
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(UwuifyCommands));

                client.BaseAddress = new Uri(settings.FortuneUri);
                client.DefaultRequestHeaders.Add("X-API-KEY", settings.FortuneApiKey);

                return client;
            });

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
    await PrepareDatabase(client);
    UpdateStartTime();

#if DEBUG
    await UpdateDebugSlashCommands(
        client.Services.GetRequiredService<DiscordSettings>(),
        client.Services.GetRequiredService<SlashService>());
#endif
}

void UpdateStartTime()
{
    var now = DateTime.Now;
    if (ShortTermMemory.StartTime < now)
        ShortTermMemory.StartTime = now;
}

static async Task PrepareDatabase(IHost client)
{
    var dataContext = client.Services.GetRequiredService<DataContext>();
    var canConnect = await dataContext.Database.CanConnectAsync();
    if (canConnect)
    {
        await dataContext.Database.MigrateAsync();
    }
    else
    {
        throw new ApplicationException("Database unreachable");
    }
}