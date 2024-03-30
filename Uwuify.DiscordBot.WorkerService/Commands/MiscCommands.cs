﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using PrimS.Telnet;

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
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService.Commands;

public partial class MiscCommands : LoggedCommandGroup<MiscCommands>
{
    private readonly DiscordSettings _settings;
    private readonly FeedbackService _feedbackService;
    private readonly TimeSpan _telnetTimeout = TimeSpan.FromSeconds(2);

    public MiscCommands(ILogger<MiscCommands> logger,
        DiscordSettings settings,
        FeedbackService feedbackService,
        ICommandContext ctx,
        IDiscordRestGuildAPI guildApi,
        IDiscordRestChannelAPI channelApi)
        : base(ctx, logger, guildApi, channelApi)
    {
        _settings = settings;
        _feedbackService = feedbackService;
    }

    [Command("eval")]
    [CommandType(ApplicationCommandType.ChatInput)]
    [Ephemeral]
    [Description("Administrator Console - For internal use only. Don't use!")]
    public async Task<IResult> FakeEvalAsync([Description("bash $")] string text)
    {
        await LogCommandUsageAsync(typeof(MiscCommands).GetMethod(nameof(FakeEvalAsync)), text);

        if (string.IsNullOrWhiteSpace(text))
        {
            var invalidReply = await _feedbackService.SendContextualErrorAsync("Not a valid input.");
            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        string descriptionOutput = $"Exit code did not indicate success.";
        string consoleOutput = "Socket error";
        var sw = new Stopwatch();
        var color = Color.Green;

        try
        {
            sw.Start();
            var telnet = new Client(_settings.HoneyPotHost, _settings.HoneyPotPort, CancellationToken);

            // Login
            await telnet.WriteLineAsync(_settings.HoneyPotUsername);
            if ((await telnet.ReadAsync(_telnetTimeout)).Contains("Password"))
            {
                await telnet.WriteLineAsync(_settings.HoneyPotPassword);
                _ = await telnet.ReadAsync(_telnetTimeout);
            }

            await telnet.WriteLineAsync(text);
            var result = await telnet.ReadAsync(_telnetTimeout);
            sw.Stop();

            if (result.Length != 0)
            {
                descriptionOutput = "Executed successfully.";

                var split = result.Split('\n');
                var sb = new StringBuilder();
                foreach (var lineSplit in split)
                {
                    var cleanedLineSplit = lineSplit
                        .Replace("\u001b[4l", string.Empty)
                        .Replace("\u001b[4h", string.Empty)
                        .Trim();
                    sb.AppendLine(cleanedLineSplit);
                }
                consoleOutput = sb.ToString();
            }
        }
        catch (Exception ex)
        {
            color = Color.Red;
            consoleOutput = ex.Message;
            _logger.LogError(ex, "Failed to invoke telnet command");
        }

        _logger.LogDebug("Responding with: {evalText}", consoleOutput);

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Admin Console (Evaluation)",
                Description: descriptionOutput,
                Fields: new List<EmbedField>
                {
                    new EmbedField("Console Output", $"```bash{Environment.NewLine}{consoleOutput}```")
                },
                Colour: new Optional<Color>(color),
                Footer: new EmbedFooter($"Execution took {sw.ElapsedMilliseconds} ms")),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }

    [Command("feedback")]
    [CommandType(ApplicationCommandType.ChatInput)]
    [Ephemeral]
    [Description("Leave feedback or report an issue for the developer to review")]
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

        _logger.LogInformation("New feedback left by {userName}. Feedback: {feedbackText}", _ctx.TryGetUser().ToFullUsername(), text.Trim());

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Feedback Submitted",
                Description: "Thank you for your feedback! The developer will review your comments shortly.",
                Colour: new Optional<Color>(Color.PaleVioletRed)),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }
}