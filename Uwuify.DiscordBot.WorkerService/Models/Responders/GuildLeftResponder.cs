using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Discord.Rest.API;
using Remora.Results;
using Uwuify.DiscordBot.WorkerService.Extensions;

namespace Uwuify.DiscordBot.WorkerService.Models.Responders;

public class GuildLeftResponder : IResponder<IGuildCreate>
{
    private readonly ILogger<GuildLeftResponder> _logger;
    private readonly IDiscordRestUserAPI _userApi;

    public GuildLeftResponder(ILogger<GuildLeftResponder> logger, IDiscordRestUserAPI userApi)
    {
        _logger = logger;
        _userApi = userApi;
    }

    public async Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = new())
    {
        _logger.LogInformation("Left guild: {guildName} ({guildId})",
            gatewayEvent.Name,
            gatewayEvent.ID);

        await _logger.LogGuildCountAsync(_userApi, ct);

        return Result.FromSuccess();
    }
}