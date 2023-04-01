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
        gatewayEvent.Guild.Switch(
            g =>
            {
                if (ShortTermMemory.KnownGuilds.Contains(g.ID))
                    return;

                _logger.LogInformation("Joined new guild (available): {guildName} ({guildId}) with {userCount} users.",
                    g.Name,
                    g.ID,
                    g.MemberCount);

                ShortTermMemory.KnownGuilds.Add(g.ID);
            },
            g =>
            {
                if (ShortTermMemory.KnownGuilds.Contains(g.ID))
                    return;

                _logger.LogInformation("Joined new guild (unavailable): {guildId}.",
                    g.ID);

                ShortTermMemory.KnownGuilds.Add(g.ID);
            });



        _logger.LogGuildCount();

        return Task.FromResult(Result.FromSuccess());
    }
}