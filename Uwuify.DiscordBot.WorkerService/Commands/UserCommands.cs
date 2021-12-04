using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uwuify.Humanizer;
using Remora.Commands.Groups;
using Remora.Commands.Attributes;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using System.ComponentModel;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;

namespace Uwuify.DiscordBot.WorkerService.Commands
{
    public class UserCommands : CommandGroup
    {
        private readonly ILogger<UserCommands> _logger;
        private readonly FeedbackService _feedbackService;
        private readonly ICommandContext _ctx;

        public UserCommands(ILogger<UserCommands> logger, FeedbackService feedbackService, ICommandContext ctx)
        {
            _logger = logger;
            _feedbackService = feedbackService;
            _ctx = ctx;
        }

        [Command("uwuify", "uwu", "owo")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Convert your message into UwU")]
        public async Task<IResult> UwuAsync([Description("Uwuify text")]string text)
        {
            var outputMsg = text.Uwuify();
            _logger.LogDebug("{commandName} result: {msg}", nameof(UwuAsync), outputMsg);
            await _feedbackService.SendContextualEmbedAsync(new Embed("Uwuify", Description: outputMsg), ct: CancellationToken);
            return Result.FromSuccess();
        }

        //[Command("Uwuify It")]
        //[CommandType(ApplicationCommandType.Message)]
        //public async Task<IResult> UwuSomeoneAsync()
        //{
        //    _ctx
        //    var outputMsg = "test".Uwuify();
        //    _logger.LogDebug("{commandName} result: {msg}", nameof(UwuAsync), outputMsg);
        //    await _feedbackService.SendContextualEmbedAsync(new Embed("Uwuify", Description: outputMsg), ct: CancellationToken);
        //    return Result.FromSuccess();
        //}

        [Command("feedback")]
        [CommandType(ApplicationCommandType.ChatInput)]
        [Description("Leave feedback for the developer")]
        public async Task<IResult> FeedbackAsync([Description("Feedback text")]string text)
        {
            _logger.LogInformation("New feedback left by {userName}. Feedback: {feedbackText}", $"{_ctx.User.Username}#{_ctx.User.Discriminator}", text.Trim());

            await _feedbackService.SendContextualEmbedAsync(new Embed("Feedback Submitted",
                Description: "Thank you for your feedback! A developer will review your comments shortly."), ct: CancellationToken);
            return Result.FromSuccess();
        }
    }
}