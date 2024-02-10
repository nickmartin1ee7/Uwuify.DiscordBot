using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;

using Uwuify.DiscordBot.WorkerService.Extensions;

namespace Uwuify.DiscordBot.WorkerService.Commands;

public class LoggedCommandGroup<TCommandGroup> : CommandGroup
    where TCommandGroup : class
{
    protected readonly ICommandContext _ctx;
    protected readonly ILogger<TCommandGroup> _logger;
    protected readonly IDiscordRestGuildAPI _guildApi;
    protected readonly IDiscordRestChannelAPI _channelApi;

    public LoggedCommandGroup(ICommandContext ctx, ILogger<TCommandGroup> logger, IDiscordRestGuildAPI guildApi, IDiscordRestChannelAPI channelApi)
    {
        _ctx = ctx;
        _logger = logger;
        _guildApi = guildApi;
        _channelApi = channelApi;
    }

    protected async Task LogCommandUsageAsync(MethodInfo callerMethod, params string[] commandArguments)
    {
        var commandName = callerMethod.CustomAttributes
            .First(a => a.AttributeType == typeof(CommandAttribute))
            .ConstructorArguments.First().Value;

        var user = _ctx.TryGetUser();

        if (!_ctx.TryGetGuildID(out var guildId))
        {
            _logger.LogInformation("{commandName} triggered by {userName} ({userId}) in DM; Message: {message}",
                commandName,
                user.ToFullUsername(),
                user.ID,
                string.Join(' ', commandArguments));
            return;
        }

        var guild = await _guildApi.GetGuildAsync(guildId, ct: CancellationToken);

        _ctx.TryGetChannelID(out var channelId);
        var channel = await _channelApi.GetChannelAsync(channelId, ct: CancellationToken);

        _logger.LogInformation("{commandName} triggered by {userName} ({userId}) in #{channel} ({channelId}); {guildName} ({guildId}); Message: {message}",
            commandName,
            user.ToFullUsername(),
            user.ID,
            channel.Entity.Name.Value,
            channel.Entity.ID,
            guild.Entity.Name,
            guild.Entity.ID,
            string.Join(' ', commandArguments));
    }
}