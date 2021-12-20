using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using Uwuify.DiscordBot.WorkerService.Extensions;

namespace Uwuify.DiscordBot.WorkerService.Models.Responders;

public class GuildJoinedResponder : IResponder<IGuildCreate>
{
    private readonly ILogger<GuildJoinedResponder> _logger;
    private readonly SlashService _slashService;
    private readonly IDiscordRestUserAPI _userApi;

    public GuildJoinedResponder(ILogger<GuildJoinedResponder> logger, SlashService slashService, IDiscordRestUserAPI userApi)
    {
        _logger = logger;
        _slashService = slashService;
        _userApi = userApi;
    }

    public async Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = new())
    {
        _logger.LogInformation("Joined new guild: {guildName} ({guildId})",
            gatewayEvent.Name,
            gatewayEvent.ID);

        await _logger.LogGuildCountAsync(_userApi, ct);

        var result = await _slashService.UpdateSlashCommandsAsync(gatewayEvent.ID, ct);

        return result.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(result.Error);
    }
}