using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
        await LogCommandUsageAsync(typeof(UwuifyCommands).GetMethod(nameof(FortuneAsync)));

        _rateLimitGuardService.StartRenewalJob();
        var user = _ctx.TryGetUser();

        if (_rateLimitGuardService.IsRateLimited(user.ID, out var nextAvailableUsage))
        {
            var duration = _discordSettings.RateLimitingUsageFallOffInMilliSeconds;
            var invalidReply = await _feedbackService.SendContextualErrorAsync(
                "Your fortune has already been told today! ".Uwuify()
                + Environment.NewLine
                + $"Try again at {nextAvailableUsage}.");

            _logger.LogInformation("Rate-limited fortune for user {userName} ({userId}). Not ready until {fortuneTimeout}",
                user.ToFullUsername(),
                user.ID,
                nextAvailableUsage);

            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        (bool success, string text) = await GenerateFortuneAsync();

        if (!success)
        {
            _logger.LogWarning("Failed to generate fortune for user");

            var invalidReply = await _feedbackService.SendContextualErrorAsync(
                "Your fortune was not clear, try again later!".Uwuify());

            return invalidReply.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(invalidReply);
        }

        _rateLimitGuardService.RecordUsage(user.ID);

        string title, outputMsg;
        var splitText = text.Split(" - ");

        if (splitText.Length == 2)
        {
            title = splitText[0].Uwuify();
            outputMsg = splitText[1].Uwuify();
        }
        else
        {
            title = "UwU Fortune";
            outputMsg = text.Uwuify();
        }

        _logger.LogDebug("{commandName} result: {message}", nameof(FortuneAsync), outputMsg);

        var reply = await _feedbackService.SendContextualEmbedAsync(new Embed(title,
                Description: outputMsg,
                Colour: new Optional<Color>(Color.PaleVioletRed)),
            ct: CancellationToken);

        return reply.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(reply);
    }

    private async Task<(bool Success, string Text)> GenerateFortuneAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/generate?discord");

            var fortuneResponse = await response.Content.ReadFromJsonAsync<FortuneResponse>();

            return (true, fortuneResponse.Fortune);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request fortune from backend");

            return (false, null);
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