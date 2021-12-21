using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;
using Uwuify.DiscordBot.WorkerService.Extensions;

namespace Uwuify.DiscordBot.WorkerService.Models.Responders;

public class UnknownResponder : IResponder<IUnknownEvent>
{
    private readonly ILogger<UnknownResponder> _logger;
    private readonly IDiscordRestUserAPI _userApi;

    public UnknownResponder(ILogger<UnknownResponder> logger,
        IDiscordRestUserAPI userApi)
    {
        _logger = logger;
        _userApi = userApi;
    }

    public async Task<Result> RespondAsync(IUnknownEvent gatewayEvent, CancellationToken ct = new())
    {
        if (gatewayEvent.TryHandleAsGuildDelete(out Snowflake guildId))
        {
            if (!ShortTermMemory.KnownGuilds.Contains(guildId))
                return Result.FromSuccess();

            _logger.LogInformation("Left guild: {guildId}",
                guildId);

            ShortTermMemory.KnownGuilds.Remove(guildId);

            await _logger.LogGuildCountAsync(_userApi, ct);
        }

        return Result.FromSuccess();
    }
}