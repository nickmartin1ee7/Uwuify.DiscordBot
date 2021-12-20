using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Uwuify.DiscordBot.WorkerService.Models.Responders;

public class GuildJoinedResponder : IResponder<IGuildCreate>
{
    private readonly ILogger<GuildJoinedResponder> _logger;

    public GuildJoinedResponder(ILogger<GuildJoinedResponder> logger)
    {
        _logger = logger;
    }

    public async Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = new())
    {
        _logger.LogInformation("Joined new guild: {guildName} ({guildId})",
            gatewayEvent.Name,
            gatewayEvent.ID);

        return Result.FromSuccess();
    }
}