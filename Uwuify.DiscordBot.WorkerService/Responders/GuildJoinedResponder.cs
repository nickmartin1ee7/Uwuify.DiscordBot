using System;
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
    private readonly DiscordSettings _settings;

    public GuildJoinedResponder(ILogger<GuildJoinedResponder> logger,
        DiscordSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = new())
    {
        if (ShortTermMemory.ShardsReady.Count != _settings.Shards)
            return Task.FromResult(Result.FromSuccess());

        gatewayEvent.Guild.Switch(
            g =>
            {
                if (ShortTermMemory.KnownGuilds.Contains(g.ID))
                    return;

                ShortTermMemory.AddKnownGuild(g);

                if (ShortTermMemory.StartTime > DateTime.Now.Subtract(TimeSpan.FromMinutes(2)))
                {
                    _logger.LogInformation(
                        "Joined new guild (available): {guildName} ({guildId}) with {userCount} users. All Known User Count: {allUserCount}.",
                        g.Name,
                        g.ID,
                        g.MemberCount,
                        ShortTermMemory.KnownUserCount);

                    _logger.LogGuildCount();
                }

            },
            g =>
            {
                if (ShortTermMemory.KnownGuilds.Contains(g.ID))
                    return;

                ShortTermMemory.AddKnownGuild(g);

                if (ShortTermMemory.StartTime > DateTime.Now.Subtract(TimeSpan.FromMinutes(2)))
                {
                    _logger.LogInformation("Joined new guild (unavailable): {guildId}.",
                        g.ID);

                    _logger.LogGuildCount();
                }
            });

        return Task.FromResult(Result.FromSuccess());
    }
}