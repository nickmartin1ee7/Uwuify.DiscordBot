using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using Uwuify.DiscordBot.WorkerService.Extensions;

namespace Uwuify.DiscordBot.WorkerService.Models.Responders;

public class ReadyResponder : IResponder<IReady>
{
    private readonly ILogger<ReadyResponder> _logger;
    private readonly DiscordGatewayClient _discordGatewayClient;
    private readonly DiscordSettings _settings;
    private readonly SlashService _slashService;

    public ReadyResponder(ILogger<ReadyResponder> logger,
        DiscordGatewayClient discordGatewayClient,
        DiscordSettings settings,
        SlashService slashService)
    {
        _logger = logger;
        _discordGatewayClient = discordGatewayClient;
        _settings = settings;
        _slashService = slashService;
    }

    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new())
    {
        void RememberInitialGuildIds()
        {
            foreach (var gatewayEventGuild in gatewayEvent.Guilds)
            {
                ShortTermMemory.KnownGuilds.Add(gatewayEventGuild.GuildID);
            }
        }

        async Task UpdateGlobalSlashCommands()
        {
            var updateResult = await _slashService.UpdateSlashCommandsAsync(ct: ct);

            if (!updateResult.IsSuccess)
            {
                _logger.LogWarning("Failed to update application commands globally");
            }
        }

        void UpdatePresence()
        {
            var updateCommand = new UpdatePresence(ClientStatus.Online, false, null, new IActivity[]
            {
                new Activity(_settings.Status, ActivityType.Watching)
            });

            _discordGatewayClient.SubmitCommand(updateCommand);
        }

        if (gatewayEvent.Shard.HasValue)
        {
            _logger.LogInformation("Shard #{shardId} of {shardCount}", gatewayEvent.Shard.Value.ShardID, gatewayEvent.Shard.Value.ShardCount);
        }

        _logger.LogInformation("{botUser} is online for {guildCount} guilds",
            gatewayEvent.User.ToFullUsername(),
            gatewayEvent.Guilds.Count);

        RememberInitialGuildIds();
        UpdatePresence();
        await UpdateGlobalSlashCommands();

        return Result.FromSuccess();
    }
}