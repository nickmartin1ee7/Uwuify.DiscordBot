using System;
using System.Collections.Generic;
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

namespace Uwuify.DiscordBot.WorkerService.Commands;

public class MiscCommands : LoggedCommandGroup<MiscCommands>
{
    private static readonly string s_fakeToken = Convert.ToBase64String(Enumerable.Range(0, 50)
        .Select(_ => (byte) Random.Shared.Next()).ToArray());

    private static readonly Dictionary<string, (int ArgCount, string CommandFormat)> s_fakeCommands = new()
    {
        ["whoami"] = (0, "root"),
        ["ls"] = (0, ".\n..\ntoken.txt"),
        ["cat"] = (1, s_fakeToken),
        ["echo"] = (1, "{0}"),
        ["help"] = (0, @"GNU bash, version 4.4.23(1)-release (x86_64-pc-msys)
These shell commands are defined internally.  Type `help' to see this list.

whoami
ls [FILE]...
cat [FILE]
echo [arg ...]
help")
    };

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

    [Command("eval")]
    [CommandType(ApplicationCommandType.ChatInput)]
    [Ephemeral]
    [Description("For internal use only")]
    public async Task<IResult> FakeEvalAsync([Description("bash $")] string text)
    {
        await LogCommandUsageAsync(typeof(MiscCommands).GetMethod(nameof(FakeEvalAsync)), text);

        string[] cmdLine;
        
        if (string.IsNullOrWhiteSpace(text) || !(cmdLine = text.Split(' ')).Any())
        {
            var invalidReply = await _feedbackService.SendContextualErrorAsync("Not a valid input.");
            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        var cmd = cmdLine[0];
        var hasCommand = s_fakeCommands.ContainsKey(cmd);
        string descriptionText;

        if (hasCommand)
        {
            descriptionText = cmdLine.Length - 1 == s_fakeCommands[cmd].ArgCount
                ? string.Format(s_fakeCommands[cmd].CommandFormat, cmdLine.Skip(1).ToArray())
                : $"bash: {cmd}: wrong amount of arguments";
        }
        else
        {
            descriptionText = $"bash: {cmd}: command not found";
        }

        _logger.LogDebug("Responding with: {evalText}", descriptionText);
        
        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Eval",
                Description: descriptionText,
                Colour: new Optional<Color>(Color.Red)),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }

    [Command("feedback")]
    [CommandType(ApplicationCommandType.ChatInput)]
    [Ephemeral]
    [Description("Leave feedback for the developer")]
    public async Task<IResult> FeedbackAsync([Description("Enter your feedback to the developer")] string text)
    {
        await LogCommandUsageAsync(typeof(MiscCommands).GetMethod(nameof(FeedbackAsync)), text);

        if (string.IsNullOrWhiteSpace(text))
        {
            var invalidReply = await _feedbackService.SendContextualErrorAsync("Your feedback must contain a message.");
            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

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