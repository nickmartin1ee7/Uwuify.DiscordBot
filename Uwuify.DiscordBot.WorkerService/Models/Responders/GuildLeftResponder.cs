using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using Uwuify.DiscordBot.WorkerService.Extensions;

namespace Uwuify.DiscordBot.WorkerService.Models.Responders;

public class GuildLeftResponder : IResponder<IGuildDelete>
{
    private readonly ILogger<GuildLeftResponder> _logger;
    private readonly IDiscordRestUserAPI _userApi;

    public GuildLeftResponder(ILogger<GuildLeftResponder> logger,
        IDiscordRestUserAPI userApi)
    {
        _logger = logger;
        _userApi = userApi;
    }

    public async Task<Result> RespondAsync(IGuildDelete gatewayEvent, CancellationToken ct = new())
    {
        // Unknown guild, or it is a Discord outage
        if (!ShortTermMemory.KnownGuilds.Contains(gatewayEvent.GuildID) || !gatewayEvent.IsUnavailable.HasValue)
            return Result.FromSuccess();
        
        _logger.LogInformation("Left guild: {guildId}",
            gatewayEvent.GuildID);

        ShortTermMemory.KnownGuilds.Remove(gatewayEvent.GuildID);

        await _logger.LogGuildCountAsync(_userApi, ct);

        return Result.FromSuccess();
    }
}