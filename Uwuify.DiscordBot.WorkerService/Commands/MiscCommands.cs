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
using System.Threading.Tasks;
using Uwuify.DiscordBot.WorkerService.Extensions;

namespace Uwuify.DiscordBot.WorkerService.Commands;

public class MiscCommands : LoggedCommandGroup<MiscCommands>
{
    private readonly FeedbackService _feedbackService;

    public MiscCommands(ILogger<MiscCommands> logger,
        FeedbackService feedbackService,
        ICommandContext ctx,
        IDiscordRestGuildAPI guildApi,
        IDiscordRestChannelAPI channelApi)
        : base(ctx, logger, guildApi, channelApi)
    {
        _feedbackService = feedbackService;
    }

    [Command("feedback")]
    [CommandType(ApplicationCommandType.ChatInput)]
    [Ephemeral]
    [Description("Leave feedback for the developer")]
    public async Task<IResult> FeedbackAsync([Description("Enter your feedback to the developer")] string text)
    {
        await LogCommandUsageAsync(typeof(MiscCommands).GetMethod(nameof(FeedbackAsync)), text);

        _logger.LogInformation("New feedback left by {userName}. Feedback: {feedbackText}", _ctx.User.ToFullUsername(), text.Trim());

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Feedback Submitted",
                Description: "Thank you for your feedback! A developer will review your comments shortly.",
                Colour: new Optional<Color>(Color.PaleVioletRed)),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }
}