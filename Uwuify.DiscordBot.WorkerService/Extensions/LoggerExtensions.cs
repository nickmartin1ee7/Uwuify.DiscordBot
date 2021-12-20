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
            logger.LogInformation("Guild Count {count}",
                await userApi.GetCurrentUserGuildsAsync(ct: ct));
        }
    }
}
