using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService.Responders;

public class GuildJoinedResponder : IResponder<IGuildCreate>
{
    private readonly ILogger<GuildJoinedResponder> _logger;

    public GuildJoinedResponder(ILogger<GuildJoinedResponder> logger)
    {
        _logger = logger;
    }

    public Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = new())
    {
        if (ShortTermMemory.KnownGuilds.Contains(gatewayEvent.ID))
            return Task.FromResult(Result.FromSuccess());

        _logger.LogInformation("Joined new guild: {guildName} ({guildId}) with {userCount} users.",
            gatewayEvent.Name,
            gatewayEvent.ID,
            gatewayEvent.MemberCount);

        ShortTermMemory.KnownGuilds.Add(gatewayEvent.ID);

        _logger.LogGuildCount();

        return Task.FromResult(Result.FromSuccess());
    }
}