using Microsoft.Extensions.Logging;

using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class LoggerExtensions
    {
        private static int s_lastLoggedGuildCount;

        public static void LogGuildCount(this ILogger logger)
        {
            if (s_lastLoggedGuildCount == ShortTermMemory.KnownGuilds.Count)
                return;

            s_lastLoggedGuildCount = ShortTermMemory.KnownGuilds.Count;

            logger.LogInformation("Guild Count: {count}",
                ShortTermMemory.KnownGuilds.Count);
        }
    }
}
