using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Humanizer;

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
using Uwuify.DiscordBot.WorkerService.Models;
using Uwuify.Humanizer;

namespace Uwuify.DiscordBot.WorkerService.Commands;

public class UwuifyCommands : LoggedCommandGroup<UwuifyCommands>
{
    private readonly FeedbackService _feedbackService;
    private readonly IProfanityFilter _profanityFilter;
    private readonly HttpClient _httpClient;
    private readonly RateLimitGuardService _rateLimitGuardService;
    private readonly DiscordSettings _discordSettings;

    public UwuifyCommands(ILogger<UwuifyCommands> logger,
        FeedbackService feedbackService,
        ICommandContext ctx,
        IDiscordRestGuildAPI guildApi,
        IDiscordRestChannelAPI channelApi,
        IProfanityFilter profanityFilter,
        HttpClient httpClient,
        RateLimitGuardService rateLimitGuardService,
        DiscordSettings discordSettings)
        : base(ctx, logger, guildApi, channelApi)
    {
        _feedbackService = feedbackService;
        _profanityFilter = profanityFilter;
        _httpClient = httpClient;
        _rateLimitGuardService = rateLimitGuardService;
        _discordSettings = discordSettings;
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

            _logger.LogDebug("Unable to uwuify: provided text was empty");

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

            _logger.LogDebug("Unable to uwuify: message has no content");

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

    [Command("fortune")]
    [CommandType(ApplicationCommandType.ChatInput)]
    [Description("Ask the UwU fortune teller for your fortune")]
    public async Task<IResult> FortuneAsync()
    {
        var sw = new Stopwatch();
        sw.Start();

        await LogCommandUsageAsync(typeof(UwuifyCommands).GetMethod(nameof(FortuneAsync)));

        _rateLimitGuardService.StartRenewalJob();
        var user = _ctx.TryGetUser();

        var (IsRateLimited, NextAvailableUsageInUtc) = await _rateLimitGuardService.IsRateLimited(user.ID);
        if (IsRateLimited && NextAvailableUsageInUtc.HasValue)
        {
            var fortuneTimeoutDuration = (DateTime.UtcNow - NextAvailableUsageInUtc.Value).Humanize();
            var fortunateTimeoutDate = new DateTimeOffset(NextAvailableUsageInUtc.Value);

            var invalidReply = await _feedbackService.SendContextualErrorAsync(
                "Your fortune has already been told today! ".Uwuify()
                + Environment.NewLine
                + $"Try again in {fortuneTimeoutDuration} (at {fortunateTimeoutDate}).");

            _logger.LogInformation("Rate-limited fortune for user {userName} ({userId}). Not ready for {fortuneTimeoutDuration} ({fortuneTimeoutDate})",
                user.ToFullUsername(),
                user.ID,
                fortuneTimeoutDuration,
                fortunateTimeoutDate);

            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        (bool success, string header, string body, string luckText) = await GenerateFortuneAsync();

        if (!success)
        {
            _logger.LogWarning("Failed to generate fortune for user");

            var invalidReply = await _feedbackService.SendContextualErrorAsync(
                "Your fortune was not clear, try again later!".Uwuify());

            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        await _rateLimitGuardService.RecordUsage(user.ID);

        var title = (!string.IsNullOrWhiteSpace(header) ? header : "UwU Fortune");
        var uwuifiedTitle = title.Uwuify();
        var uwuifiedBody = body.Uwuify();
        var uwuifiedLuckText = $"Your Luck is {luckText}".Uwuify();

        sw.Stop();

        _logger.LogDebug("{commandName} result: {message}. ElapsedMs: {elapsedMs}", nameof(FortuneAsync), uwuifiedBody, sw.ElapsedMilliseconds);

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed($"🔮 {uwuifiedTitle} 🥠",
                Fields:
                new EmbedField[]
                {
                    new(Name: $"🍀 {uwuifiedLuckText} ⛈️", Value: uwuifiedBody)
                },
                Footer: new EmbedFooter($"{title} ({luckText}) - {body} ({sw.Elapsed.Milliseconds} ms)"),
                Colour: new Optional<Color>(Color.PaleVioletRed)),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }

    private async Task<(bool Success, string Header, string Body, string LuckText)> GenerateFortuneAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/generate?discord");
            var generateResponse = await response.Content.ReadFromJsonAsync<GenerateResponse>();

            return (true, generateResponse.fortune.header, generateResponse.fortune.body, generateResponse.luckText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request fortune from backend");

            return default;
        }
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