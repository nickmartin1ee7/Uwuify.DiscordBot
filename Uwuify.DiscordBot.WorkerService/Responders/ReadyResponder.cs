using System.Linq;
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
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService.Responders;

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
            ShortTermMemory.ShardsReady.Add(gatewayEvent.Shard.Value.ShardID);

            foreach (var gatewayEventGuild in gatewayEvent.Guilds)
            {
                ShortTermMemory.AddKnownGuild(gatewayEventGuild);
            }
        }

        void UpdatePresence()
        {
            if (string.IsNullOrWhiteSpace(_settings.Status))
                return;

            var updateCommand = new UpdatePresence(UserStatus.Online, false, null,
            [
                new Activity(_settings.Status, ActivityType.Watching)
            ]);

            _discordGatewayClient.SubmitCommand(updateCommand);
        }

        async Task UpdateGlobalSlashCommands()
        {
            var updateResult = await _slashService.UpdateSlashCommandsAsync(ct: ct);

            if (!updateResult.IsSuccess)
            {
                _logger.LogWarning("Failed to update application commands globally");
            }
        }

        void LogClientDetails()
        {
            _logger.LogInformation(
                "{botUser} (Shard #{shardId}) is online for {shardGuildCount} guilds.",
                gatewayEvent.User.ToFullUsername(),
                gatewayEvent.Shard.Value.ShardID,
                gatewayEvent.Guilds.Count);

            if (!gatewayEvent.Guilds.Any())
                _logger.LogWarning("Shard #{shardId} has no guilds!",
                    gatewayEvent.Shard.Value.ShardID);
        }

        RememberInitialGuildIds();

        if (gatewayEvent.Shard.HasValue)
        {
            _logger.LogInformation("Shard Id (#{shardId}) ready ({shardIndex} of {shardCount}).",
                gatewayEvent.Shard.Value.ShardID,
                gatewayEvent.Shard.Value.ShardID + 1,
                gatewayEvent.Shard.Value.ShardCount);

            // Last shard should log guild count
            if (ShortTermMemory.ShardsReady.Count == gatewayEvent.Shard.Value.ShardCount)
                _logger.LogGuildCount();

            // First shard can update global state
            if (gatewayEvent.Shard.Value.ShardID == 0)
            {
                UpdatePresence();
                await UpdateGlobalSlashCommands();
            }
        }

        LogClientDetails();

        return Result.FromSuccess();
    }
}