using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ProfanityFilter.Interfaces;

using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.Humanizer;

namespace Uwuify.DiscordBot.WorkerService.Commands;

public class UwuifyCommands : LoggedCommandGroup<UwuifyCommands>
{
    private readonly FeedbackService _feedbackService;
    private readonly IProfanityFilter _profanityFilter;

    public UwuifyCommands(ILogger<UwuifyCommands> logger,
        FeedbackService feedbackService,
        ICommandContext ctx,
        IDiscordRestGuildAPI guildApi,
        IDiscordRestChannelAPI channelApi,
        IProfanityFilter profanityFilter)
        : base(ctx, logger, guildApi, channelApi)
    {
        _feedbackService = feedbackService;
        _profanityFilter = profanityFilter;
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

        string outputMsg = CensorAndUwuify(text);
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
    public async Task<IResult> UwuThisMessageAsync(string message)
    {
        var c = _ctx as InteractionContext;

        var interactionData = c!.Interaction.Data.Value.AsT0.Resolved.Value.Messages.Value.Values.First();
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

        var outputMsg = CensorAndUwuify(originalMessage);

        _logger.LogDebug("{commandName} result: {message}", nameof(UwuThisMessageAsync), outputMsg);

        var targetUser = c.Interaction.Data.Value.AsT0.Resolved.Value.Messages.Value.First().Value.Author.Value.ToFullUsername();

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed($"{targetUser} Just Got UwU-ed!",
                Description: outputMsg,
                Colour: new Optional<Color>(Color.PaleVioletRed)),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }

    private string CensorAndUwuify(string text)
    {
        text = text.ToLower();
        return _profanityFilter.ContainsProfanity(text)
            ? _profanityFilter
                .CensorString(text)
                .Uwuify(0)
                .Replace("*", "\\*")// Avoid *-*** situations
            : text.Uwuify();
    }
}