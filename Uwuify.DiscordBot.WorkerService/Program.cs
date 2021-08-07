using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using Uwuify.DiscordBot.WorkerService.Models;
using Uwuify.DiscordBot.WorkerService.Services;

namespace Uwuify.DiscordBot.WorkerService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
#if DEBUG
                .AddJsonFile("appsettings.Development.json")
#else
                .AddJsonFile("appsettings.json")
#endif
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();


            var host = CreateHostBuilder(configuration, args).Build();

            try
            {
                host.Run();
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

        public static IHostBuilder CreateHostBuilder(IConfiguration configuration, string[] args)
        {

            return Host.CreateDefaultBuilder(args)
                .UseSerilog(Log.Logger)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddSingleton<DiscordSettings>(configuration.GetSection(nameof(DiscordSettings)).Get<DiscordSettings>());
                    services.AddSingleton<DiscordSocketClient>();
                    services.AddSingleton<CommandHandlingService>();
                    services.AddSingleton<CommandService>();
                    services.AddSingleton<DiscordBotClient>();
                    services.AddHostedService<Worker>();
                });
        }
    }
}
