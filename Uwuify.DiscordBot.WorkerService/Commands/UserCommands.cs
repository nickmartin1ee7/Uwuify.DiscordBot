using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using System.ComponentModel;
using System.Threading.Tasks;
using Uwuify.Humanizer;

namespace Uwuify.DiscordBot.WorkerService.Commands
{
    public class UserCommands : CommandGroup
    {
        private readonly ILogger<UserCommands> _logger;
        private readonly FeedbackService _feedbackService;
        private readonly ICommandContext _ctx;

        public UserCommands(ILogger<UserCommands> logger,
            FeedbackService feedbackService,
            ICommandContext ctx)
        {
            _logger = logger;
            _feedbackService = feedbackService;
            _ctx = ctx;
        }

        [Command("uwuify")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Convert your message into UwU")]
        public async Task<IResult> UwuAsync([Description("text")] string text)
        {
            var outputMsg = text.Uwuify();
            
            _logger.LogDebug("{commandName} result: {msg}", nameof(UwuAsync), outputMsg);
            
            var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Uwuify", Description: outputMsg), ct: CancellationToken);
            
            return reply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(reply);
        }

        [Command("feedback")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Leave feedback for the developer")]
        public async Task<IResult> FeedbackAsync([Description("text")] string text)
        {
            _logger.LogInformation("New feedback left by {userName}. Feedback: {feedbackText}", $"{_ctx.User.Username}#{_ctx.User.Discriminator}", text.Trim());

            var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Feedback Submitted",
                Description: "Thank you for your feedback! A developer will review your comments shortly."), ct: CancellationToken);

            return reply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(reply);
        }

        [Command("Uwuify This")]
        [CommandType(ApplicationCommandType.Message)]
        public async Task<IResult> UwuThisAsync()
        {
            var c = _ctx as InteractionContext;

            var outputMsg = c?.Message.Value.Content.Uwuify();

            _logger.LogDebug("{commandName} result: {msg}", nameof(UwuAsync), outputMsg);

            var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Uwuify", Description: outputMsg),
                ct: CancellationToken);

            return reply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(reply);
        }
    }
}