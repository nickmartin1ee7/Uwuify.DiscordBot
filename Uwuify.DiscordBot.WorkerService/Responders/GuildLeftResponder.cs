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

public class GuildLeftResponder : IResponder<IGuildDelete>
{
    private readonly ILogger<GuildLeftResponder> _logger;

    public GuildLeftResponder(ILogger<GuildLeftResponder> logger)
    {
        _logger = logger;
    }

    public Task<Result> RespondAsync(IGuildDelete gatewayEvent, CancellationToken ct = new())
    {
        if (!ShortTermMemory.KnownGuilds.Contains(gatewayEvent.ID) // Haven't seen this guild before
            || gatewayEvent.IsUnavailable.HasValue) // If the unavailable field is not set, the user was removed from the guild.
            return Task.FromResult(Result.FromSuccess());

        ShortTermMemory.KnownGuilds.Remove(gatewayEvent.ID);

        if (ShortTermMemory.StartTime > DateTime.Now.Subtract(TimeSpan.FromMinutes(2)))
        {
            _logger.LogInformation("Left guild: {guildId}",
                gatewayEvent.ID);

            _logger.LogGuildCount();
        }

        return Task.FromResult(Result.FromSuccess());
    }
}