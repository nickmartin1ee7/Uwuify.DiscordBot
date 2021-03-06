using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

        var guildName = await _guildApi.GetGuildAsync(_ctx.GuildID.Value, ct: CancellationToken);

        var channelName = await _channelApi.GetChannelAsync(_ctx.ChannelID, ct: CancellationToken);

        _logger.LogInformation("{commandName} triggered by {userName} ({userId}) in #{channel} ({channelId}); {guildName} ({guildId}); Message: {message}",
            commandName,
            _ctx.User.ToFullUsername(),
            _ctx.User.ID,
            channelName.Entity.Name.Value,
            _ctx.ChannelID,
            guildName.Entity.Name,
            _ctx.GuildID.Value,
            string.Join(' ', commandArguments));
    }
}