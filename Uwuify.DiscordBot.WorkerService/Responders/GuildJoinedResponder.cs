using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using System.Threading;
using System.Threading.Tasks;
using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService.Responders;

public class GuildJoinedResponder : IResponder<IGuildCreate>
{
    private readonly ILogger<GuildJoinedResponder> _logger;
    private readonly IDiscordRestUserAPI _userApi;

    public GuildJoinedResponder(ILogger<GuildJoinedResponder> logger,
        IDiscordRestUserAPI userApi)
    {
        _logger = logger;
        _userApi = userApi;
    }

    public async Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = new())
    {
        if (ShortTermMemory.KnownGuilds.Contains(gatewayEvent.ID))
            return Result.FromSuccess();

        int? userCount = null;

        if (gatewayEvent.MemberCount.HasValue)
            userCount = gatewayEvent.MemberCount.Value;
        else if (gatewayEvent.ApproximateMemberCount.HasValue)
            userCount = gatewayEvent.ApproximateMemberCount.Value;

        if (userCount.HasValue)
            _logger.LogInformation("Joined new guild: {guildName} ({guildId}) with {userCount} users",
                gatewayEvent.Name,
                gatewayEvent.ID,
                userCount);
        else
            _logger.LogInformation("Joined new guild: {guildName} ({guildId})",
                gatewayEvent.Name,
                gatewayEvent.ID);

        ShortTermMemory.KnownGuilds.Add(gatewayEvent.ID);

        await _logger.LogGuildCountAsync(_userApi, ct);

        return Result.FromSuccess();
    }
}