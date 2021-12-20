using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Uwuify.DiscordBot.WorkerService.Models
{
    public class ReadyResponder : IResponder<IReady>
    {
        private readonly DiscordGatewayClient _discordGatewayClient;
        private readonly DiscordSettings _settings;

        public ReadyResponder(DiscordGatewayClient discordGatewayClient, DiscordSettings settings)
        {
            _discordGatewayClient = discordGatewayClient;
            _settings = settings;
        }

        public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new())
        {
            var updateCommand = new UpdatePresence(ClientStatus.Online, false, null, new IActivity[]
            {
                new Activity(_settings.Status, ActivityType.Watching)
            });

            _discordGatewayClient.SubmitCommand(updateCommand);

            return Result.FromSuccess();
        }
    }
}
