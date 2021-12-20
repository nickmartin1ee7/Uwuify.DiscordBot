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
using System.Threading.Tasks;
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

        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(Log.Logger)
            .ConfigureServices(services =>
            {
                // Discord
                services
                    .AddDiscordCommands(true)
                    .AddCommandGroup<UserCommands>();

                // Configuration
                services
                    .AddSingleton(configuration)
                    .AddSingleton(configuration
                        .GetSection(nameof(DiscordSettings))
                        .Get<DiscordSettings>());

                var responderTypes = typeof(Program).Assembly
                    .GetExportedTypes()
                    .Where(t => t.IsResponder());

                foreach (var responderType in responderTypes)
                {
                    services.AddResponder(responderType);
                }
            })
            .AddDiscordService(_ => token)
            .Build();

        ValidateSlashCommandSupport(host);
#if DEBUG
        await UpdateDebugSlashCommands(host);
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

    private static void ValidateSlashCommandSupport(IHost host)
    {
        var slashService = host.Services.GetRequiredService<SlashService>();
        var checkSlashSupport = slashService.SupportsSlashCommands();
        if (!checkSlashSupport.IsSuccess)
        {
            Log.Logger.Warning(
                "The registered commands of the bot don't support slash commands: {Reason}",
                checkSlashSupport.Error!.Message);
        }
    }

    private static async Task UpdateDebugSlashCommands(IHost host)
    {
        var debugServerString = host.Services.GetRequiredService<DiscordSettings>().DebugServerId;

        if (!Snowflake.TryParse(debugServerString, out var debugServer))
        {
            Log.Logger.Warning("Failed to parse debug server from configuration!");
        }

        var slashService = host.Services.GetRequiredService<SlashService>();
        var updateSlash = await slashService.UpdateSlashCommandsAsync(debugServer);
        if (!updateSlash.IsSuccess)
        {
            Log.Logger.Warning(
                "Failed to update slash commands: {Reason}",
                updateSlash.Error!.Message);
        }
    }
}
