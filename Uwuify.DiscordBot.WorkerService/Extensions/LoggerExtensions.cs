using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class LoggerExtensions
    {
        private static int s_lastGuildCount;

        public static async Task LogGuildCountAsync(this ILogger logger, IDiscordRestUserAPI userApi, CancellationToken ct = new())
        {
            var userGuilds = await userApi.GetCurrentUserGuildsAsync(ct: ct);
            
            if (!userGuilds.IsSuccess) return;
            
            var count = userGuilds.Entity.Count;

            if (s_lastGuildCount == count) return;
            
            s_lastGuildCount = count;

            logger.LogInformation("Guild Count: {count}",
                count);
        }
    }
}
