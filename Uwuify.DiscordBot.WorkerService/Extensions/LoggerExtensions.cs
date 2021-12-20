using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class LoggerExtensions
    {
        public static async Task LogGuildCountAsync(this ILogger logger, IDiscordRestUserAPI userApi, CancellationToken ct = new())
        {
            var userGuilds = await userApi.GetCurrentUserGuildsAsync(ct: ct);
            var count = userGuilds.Entity.Count;

            logger.LogInformation("Guild Count: {count}",
                count);
        }
    }
}
