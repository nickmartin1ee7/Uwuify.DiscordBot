using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService.Responders;

public class ReadyResponder : IResponder<IReady>
{
    private readonly ILogger<ReadyResponder> _logger;
    private readonly DiscordGatewayClient _discordGatewayClient;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly DiscordSettings _settings;
    private readonly SlashService _slashService;

    public ReadyResponder(ILogger<ReadyResponder> logger,
        DiscordGatewayClient discordGatewayClient,
        IDiscordRestGuildAPI guildApi,
        DiscordSettings settings,
        SlashService slashService)
    {
        _logger = logger;
        _discordGatewayClient = discordGatewayClient;
        _guildApi = guildApi;
        _settings = settings;
        _slashService = slashService;
    }

    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new())
    {
        void RememberInitialGuildIds()
        {
            foreach (var gatewayEventGuild in gatewayEvent.Guilds)
            {
                ShortTermMemory.KnownGuilds.Add(gatewayEventGuild.ID);
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

        async Task UpdateGlobalSlashCommands()
        {
            var updateResult = await _slashService.UpdateSlashCommandsAsync(ct: ct);

            if (!updateResult.IsSuccess)
            {
                _logger.LogWarning("Failed to update application commands globally");
            }
        }

        async Task LogClientDetailsAsync()
        {
            var shardUserCount = 0;
            var shardGuilds = new List<Snowflake>(gatewayEvent.Guilds.Count);

            foreach (var guild in gatewayEvent.Guilds)
            {
                shardGuilds.Add(guild.ID);

                var guildPreview = await _guildApi.GetGuildPreviewAsync(guild.ID, ct);

                if (!guildPreview.IsSuccess)
                    continue;

                shardUserCount += guildPreview.Entity.ApproximateMemberCount.HasValue
                    ? guildPreview.Entity.ApproximateMemberCount.Value
                    : 0;
            }

            _logger.LogInformation(
                "{botUser} is online for {shardGuildCount} guilds and {shardUserCount} users. Guilds: {guilds}",
                gatewayEvent.User.ToFullUsername(),
                gatewayEvent.Guilds.Count,
                shardGuilds,
                shardUserCount);
        }

        if (gatewayEvent.Shard.HasValue)
        {
            _logger.LogInformation("Shard Id (#{shardId}) ready ({shardIndex} of {shardCount}).", gatewayEvent.Shard.Value.ShardID, gatewayEvent.Shard.Value.ShardID + 1, gatewayEvent.Shard.Value.ShardCount);
        }

        RememberInitialGuildIds();
        UpdatePresence();
        await UpdateGlobalSlashCommands();
        await LogClientDetailsAsync();

        return Result.FromSuccess();
    }
}