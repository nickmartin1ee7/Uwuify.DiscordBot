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
using System.Threading.Tasks;
using Remora.Discord.Gateway.Extensions;
using Uwuify.DiscordBot.WorkerService.Commands;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
#if DEBUG
                .AddJsonFile("appsettings.Development.json")
#else
                .AddJsonFile("appsettings.json")
#endif
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Seq("http://seq:80",
                    apiKey: configuration.GetSection("Serilog")
                        .GetValue<string>("ApiKey"))
                .ReadFrom.Configuration(configuration)
                .CreateLogger();


            var host = CreateHostBuilder(configuration, args).Build();

#if DEBUG
            var debugServerString = host.Services.GetRequiredService<DiscordSettings>().DebugServerId;
            if (!Snowflake.TryParse(debugServerString, out var debugServer))
            {
                Log.Logger.Warning("Failed to parse debug server from environment");
            }
#endif

            var slashService = host.Services.GetRequiredService<SlashService>();

            var checkSlashSupport = slashService.SupportsSlashCommands();
            if (!checkSlashSupport.IsSuccess)
            {
                Log.Logger.Warning(
                    "The registered commands of the bot don't support slash commands: {Reason}",
                    checkSlashSupport.Error!.Message);
            }
            else
            {
                var updateSlash = await slashService.UpdateSlashCommandsAsync(debugServer);
                if (!updateSlash.IsSuccess)
                {
                    Log.Logger.Warning(
                        "Failed to update slash commands: {Reason}", 
                        updateSlash.Error!.Message);
                }
            }

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

        private static IHostBuilder CreateHostBuilder(IConfiguration configuration, string[] args)
        {
            var token = configuration
                .GetSection(nameof(DiscordSettings))
                .GetValue<string>("Token");

            return Host.CreateDefaultBuilder(args)
                .UseSerilog(Log.Logger)
                .AddDiscordService(_ => token)
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton(configuration);
                    services.AddSingleton(configuration
                        .GetSection(nameof(DiscordSettings))
                        .Get<DiscordSettings>());
                    services.AddDiscordCommands()
                        .AddCommandGroup<UserCommands>();
                    services.AddDiscordGateway(_ => token);
                    services.AddTransient<SlashService>();
                });
        }
    }
}