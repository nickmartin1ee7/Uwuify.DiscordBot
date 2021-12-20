using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Uwuify.DiscordBot.WorkerService.Models.Responders;

public class ReadyResponder : IResponder<IReady>
{
    private readonly ILogger<ReadyResponder> _logger;
    private readonly DiscordGatewayClient _discordGatewayClient;
    private readonly DiscordSettings _settings;

    public ReadyResponder(ILogger<ReadyResponder> logger, DiscordGatewayClient discordGatewayClient, DiscordSettings settings)
    {
        _logger = logger;
        _discordGatewayClient = discordGatewayClient;
        _settings = settings;
    }

    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new())
    {
        _logger.LogInformation("{botUser} is online for {guildCount} guilds",
            $"{gatewayEvent.User.Username}#{gatewayEvent.User.Discriminator}",
            gatewayEvent.Guilds.Count);

        var updateCommand = new UpdatePresence(ClientStatus.Online, false, null, new IActivity[]
        {
            new Activity(_settings.Status, ActivityType.Watching)
        });

        _discordGatewayClient.SubmitCommand(updateCommand);

        return Result.FromSuccess();
    }
}