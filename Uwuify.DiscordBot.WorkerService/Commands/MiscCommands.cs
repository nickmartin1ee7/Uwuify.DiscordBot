using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

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

using Uwuify.DiscordBot.WorkerService.Extensions;

namespace Uwuify.DiscordBot.WorkerService.Commands;

public class MiscCommands : LoggedCommandGroup<MiscCommands>
{
    private static readonly string s_fakeToken = Convert.ToBase64String(Enumerable.Range(0, 50)
        .Select(_ => (byte)Random.Shared.Next()).ToArray());

    private static readonly Dictionary<string, (int ArgCount, Func<string, string> Evaluator)> s_fakeCommands = new()
    {
        ["cat"] = (1, fileName => fileName switch
        {
            "..." => "Oh dear! Aren't you clever? Get your hands out of my honey pot!",
            "token.txt" => s_fakeToken,
            _ => $"cat: {fileName}: No such file or directory"
        }),
        ["echo"] = (-1, input => input),
        ["ls"] = (0, _ => ".\n..\n...\ntoken.txt"),
        ["reboot"] = (1, _ => "Failed to write reboot parameter file: Permission denied"),
        ["shutdown"] = (1, _ => "Failed to write shutdown parameter file: Permission denied"),
        ["sudo"] = (-1, _ => "Not in the sudoers file.  This incident will be reported."),
        ["whoami"] = (0, _ => "root"),
        ["help"] = (0, _ => @"GNU bash, version 4.4.23(1)-release (x86_64-pc-msys)
These shell commands are defined internally.  Type `help` to see this list.

cat [FILE]
echo [arg ...]
help
ls
reboot [TIME]
shutdown [TIME]
whoami
")
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

        string[] splitText;

        if (string.IsNullOrWhiteSpace(text) || !(splitText = text.Split(' ')).Any())
        {
            var invalidReply = await _feedbackService.SendContextualErrorAsync("Not a valid input.");
            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        var command = splitText[0];

        string consoleOutput;
        string descriptionOutput = $"Exit code did not indicate success.{Environment.NewLine}Try \"**help**\" to display information about builtin commands.";
        int exitCode = 0;

        if (s_fakeCommands.ContainsKey(command))
        {
            if (s_fakeCommands[command].ArgCount == -1 // Unlimited args
                && splitText.Length - 1 > 0 // But has at least one
                || splitText.Length - 1 == s_fakeCommands[command].ArgCount) // Or matches explicitly
            {
                descriptionOutput = "System responded successfully.";
                consoleOutput = s_fakeCommands[command].Evaluator(string.Join(' ', splitText[1..]));
            }
            else
            {
                exitCode = 1;
                consoleOutput = $"bash: {command}: wrong amount of arguments";
            }
        }
        else
        {
            exitCode = 127;
            consoleOutput = $"bash: {command}: command not found";
        }

        _logger.LogDebug("Responding with: {evalText}", consoleOutput);

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Admin Console (Evaluation)",
                Description: descriptionOutput,
                Fields: new List<EmbedField>
                {
                    new EmbedField("Exit Code", $"`{exitCode}`"),
                    new EmbedField("Console Output", $"```bash{Environment.NewLine}{consoleOutput}```")
                },
                Colour: new Optional<Color>(Color.Red),
                Footer: new EmbedFooter($"Execution took {Random.Shared.Next(8, 35) + Random.Shared.NextDouble():N2} ms")),
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