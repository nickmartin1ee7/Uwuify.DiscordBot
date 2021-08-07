using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Uwuify.DiscordBot.WorkerService.Models;
using Uwuify.DiscordBot.WorkerService.Services;

namespace Uwuify.DiscordBot.WorkerService
{
    public static class Program
    {
        private static ILogger _logger = new LoggerFactory()
            .CreateLogger(nameof(Program));

        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            try
            {
                host.Run();
            }
            catch (Exception e)
            {
                _logger.LogCritical("Hosted service crashed! Exception: {e}", e);
                throw;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var configuration = new ConfigurationBuilder()
#if DEBUG
                .AddJsonFile("appsettings.Development.json")
#else

                .AddJsonFile("appsettings.json")
#endif
                .Build();

            return Host.CreateDefaultBuilder(args)
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
