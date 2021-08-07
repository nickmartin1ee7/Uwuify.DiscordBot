using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Uwuify.DiscordBot.WorkerService.Services
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DiscordBotClient _client;

        public Worker(ILogger<Worker> logger, DiscordBotClient client)
        {
            _logger = logger;
            _client = client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting service...");

            await _client.Run(stoppingToken);

            _logger.LogInformation("Stopping service...");
        }
    }
}
