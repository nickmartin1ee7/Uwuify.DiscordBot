using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.Humanizer;

namespace Uwuify.DiscordBot.WorkerService.Commands;

public class UwuifyCommands : LoggedCommandGroup<UwuifyCommands>
{
    private readonly FeedbackService _feedbackService;

    public UwuifyCommands(ILogger<UwuifyCommands> logger,
        FeedbackService feedbackService,
        ICommandContext ctx,
        IDiscordRestGuildAPI guildApi,
        IDiscordRestChannelAPI channelApi)
        : base(ctx, logger, guildApi, channelApi)
    {
        _feedbackService = feedbackService;
    }

    [Command("uwuify")]
    [CommandType(ApplicationCommandType.ChatInput)]
    [Description("Convert your message into UwU")]
    public async Task<IResult> UwuAsync([Description("Now say something kawaii~")] string text)
    {
        await LogCommandUsageAsync(typeof(UwuifyCommands).GetMethod(nameof(UwuAsync)), text);
        
        if (string.IsNullOrWhiteSpace(text))
        {
            var invalidReply = await _feedbackService.SendContextualErrorAsync("I don't see any message to UwUify.".Uwuify());
            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        var outputMsg = text.Uwuify();

        _logger.LogDebug("{commandName} result: {message}", nameof(UwuAsync), outputMsg);

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Uwuify",
                Description: outputMsg,
                Colour: new Optional<Color>(Color.PaleVioletRed)),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }

    [Command("Uwuify This Message")]
    [CommandType(ApplicationCommandType.Message)]
    public async Task<IResult> UwuThisMessageAsync()
    {
        var c = _ctx as InteractionContext;

        var interactionData = c!.Data.AsT0.Resolved.Value.Messages.Value.Values.First();
        var originalMessage = interactionData.Content.Value;

        if (string.IsNullOrWhiteSpace(originalMessage) && interactionData.Embeds.Value.Any())
        {
            originalMessage = interactionData.Embeds.Value.First().Description.Value;
        }

        if (string.IsNullOrWhiteSpace(originalMessage))
        {
            var invalidReply = await _feedbackService.SendContextualErrorAsync("I don't see any message to UwUify.".Uwuify());
            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        await LogCommandUsageAsync(typeof(UwuifyCommands).GetMethod(nameof(UwuThisMessageAsync)), originalMessage);

        var outputMsg = originalMessage.Uwuify();

        _logger.LogDebug("{commandName} result: {message}", nameof(UwuThisMessageAsync), outputMsg);

        var targetUser = c.Data.AsT0.Resolved.Value.Messages.Value.First().Value.Author.Value.ToFullUsername();

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed($"{targetUser} Just Got UwU-ed!",
                Description: outputMsg,
                Colour: new Optional<Color>(Color.PaleVioletRed)),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }
}