using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Discord.Rest.API;
using Remora.Results;
using Uwuify.DiscordBot.WorkerService.Extensions;

namespace Uwuify.DiscordBot.WorkerService.Models.Responders;

public class ReadyResponder : IResponder<IReady>
{
    private readonly ILogger<ReadyResponder> _logger;
    private readonly DiscordGatewayClient _discordGatewayClient;
    private readonly DiscordSettings _settings;
    private readonly SlashService _slashService;
    private readonly IDiscordRestGuildAPI _guildApi;

    public ReadyResponder(ILogger<ReadyResponder> logger,
        DiscordGatewayClient discordGatewayClient,
        DiscordSettings settings,
        SlashService slashService,
        IDiscordRestGuildAPI guildApi)
    {
        _logger = logger;
        _discordGatewayClient = discordGatewayClient;
        _settings = settings;
        _slashService = slashService;
        _guildApi = guildApi;
    }

    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new())
    {
        _logger.LogInformation("{botUser} is online for {guildCount} guilds",
            gatewayEvent.User.ToFullUsername(),
            gatewayEvent.Guilds.Count);

        var updateCommand = new UpdatePresence(ClientStatus.Online, false, null, new IActivity[]
        {
            new Activity(_settings.Status, ActivityType.Watching)
        });

        _discordGatewayClient.SubmitCommand(updateCommand);

        foreach (var guild in gatewayEvent.Guilds)
        {
            var updateResult = await _slashService.UpdateSlashCommandsAsync(guild.GuildID, ct);

            if (!updateResult.IsSuccess)
            {
                var guildResult = await _guildApi.GetGuildAsync(guild.GuildID, ct: ct);
                var guildName = guildResult.Entity.Name;

                _logger.LogWarning("Failed to update application commands on guild: {guild} ({guildId})",
                    guildName,
                    guild.GuildID);
            }
        }

        return Result.FromSuccess();
    }
}