using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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
    private const int MAX_DISCORD_DESCRIPTION_LENGTH = 4_000;

    private readonly DiscordSettings _settings;
    private readonly FeedbackService _feedbackService;
    private readonly TimeSpan _cmdTelnetTimeout = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _authTelnetTimeout = TimeSpan.FromSeconds(30);

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
    public async Task<IResult> FakeEvalAsync([Description("$")] string text)
    {
        await LogCommandUsageAsync(typeof(MiscCommands).GetMethod(nameof(FakeEvalAsync)), text);

        if (string.IsNullOrWhiteSpace(text))
        {
            var invalidReply = await _feedbackService.SendContextualErrorAsync("Not a valid input.");
            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        string consoleOutput = "Socket error";
        var sw = new Stopwatch();
        var color = Color.Red;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

        try
        {
            sw.Start();
            using var telnet = new Client(_settings.HoneyPotHost, _settings.HoneyPotPort, cts.Token);

            var (IsLoggedIn, NextPrefix) = await TryLogin(telnet);
            if (!IsLoggedIn)
            {
                throw new UnauthorizedAccessException("Access denied");
            }

            await telnet.WriteLineAsync(text);
            var result = await telnet.ReadAsync(_cmdTelnetTimeout);
            sw.Stop();

            if (result.Length != 0)
            {
                var split = result.Split('\n');
                var sb = new StringBuilder(CleanLine(NextPrefix) + ' ');

                foreach (var lineSplit in split)
                {
                    var cleanedLineSplit = CleanLine(lineSplit);
                    sb.AppendLine(cleanedLineSplit);
                }

                if (sb.Length > MAX_DISCORD_DESCRIPTION_LENGTH)
                {
                    const string Etcetera = "[TRUNCATED]";
                    sb.Remove(MAX_DISCORD_DESCRIPTION_LENGTH, sb.Length - MAX_DISCORD_DESCRIPTION_LENGTH);
                    sb.Append(Etcetera);
                }
                consoleOutput = sb.ToString();
                color = Color.Green;
            }
        }
        catch (Exception ex)
        {
            consoleOutput = ex.Message;
            _logger.LogError(ex, "Failed to invoke telnet command");
        }
        finally
        {
            cts.Cancel();
        }


        _logger.LogDebug("Responding with: {evalText}", consoleOutput);

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed("Admin Console (Evaluation)",
                Description:
                    $"**Console Output**" +
                    $"```bash" +
                    $"{Environment.NewLine}" +
                    $"{consoleOutput}" +
                    $"{Environment.NewLine}" +
                    $"```",
                Colour: new Optional<Color>(color),
                Footer: new EmbedFooter($"Execution took {sw.ElapsedMilliseconds} ms")),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }

    private static string CleanLine(string lineSplit)
    {
        return lineSplit
            .Replace("\u001b[4l", string.Empty)
            .Replace("\u001b[4h", string.Empty)
            .Trim();
    }

    private async Task<(bool IsLoggedIn, string NextPrefix)> TryLogin(Client telnet)
    {
        const string PROMPT_USERNAME = "login: ^C";
        const string PROMPT_PASSWORD = "Password: ";

        var usernamePrompt = await telnet.ReadAsync(_authTelnetTimeout);
        if (usernamePrompt.Equals(PROMPT_USERNAME))
        {
            await telnet.WriteLineAsync(_settings.HoneyPotUsername);
        }

        var passwordPrompt = await telnet.ReadAsync(_authTelnetTimeout);
        if (passwordPrompt.Equals(PROMPT_PASSWORD))
        {
            await telnet.WriteLineAsync(_settings.HoneyPotPassword);
        }

        var authResultPrompt = await telnet.ReadAsync(_authTelnetTimeout);
        if (authResultPrompt.Contains(PROMPT_USERNAME)
            || authResultPrompt.Contains(PROMPT_PASSWORD))
        {
            return (false, null);
        }

        var nextPrefix = authResultPrompt.Split('\n').Last();

        if (nextPrefix.Contains($"{_settings.HoneyPotUsername}@")
            && nextPrefix.Contains(":~#"))
        {
            return (true, nextPrefix);
        }

        return (false, null);
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