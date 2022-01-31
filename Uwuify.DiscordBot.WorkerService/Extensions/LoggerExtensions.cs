using Microsoft.Extensions.Logging;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class LoggerExtensions
    {
        public static void LogGuildCount(this ILogger logger)
        {
            logger.LogInformation("Guild Count: {count}",
                ShortTermMemory.KnownGuilds.Count);
        }
    }
}
