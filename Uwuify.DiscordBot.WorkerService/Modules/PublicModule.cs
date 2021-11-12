using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.DiscordBot.WorkerService.Models;
using Uwuify.Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Uwuify.DiscordBot.WorkerService.Modules
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<PublicModule> _logger = Program.Services.GetService<ILogger<PublicModule>>();

        [Command("ping", RunMode = RunMode.Async)]
        [Alias("latency")]
        [Summary("Returns the latency to Discord.")]
        public async Task PingAsync() =>
            await Context.Message.ReplyAsync(embed: $"{Context.Client.Latency} ms"
                .ToDefaultEmbed(Context, "Latency :ping_pong:"));

        [Command("echo", RunMode = RunMode.Async)]
        [Summary("Repeats after you... Duh!")]
        public async Task EchoAsync([Remainder] string text) =>
            await Context.Message.ReplyAsync(embed: text
                .ToDefaultEmbed(Context, "Echo"));

        // Uwuify your message, and optionally someone else's message
        [Command("uwuify", RunMode = RunMode.Async)]
        [Alias("uwu", "owo")]
        [Summary("Uwuify your message! Also, can Uwuify someone else by replying to them.")]
        public async Task UwuAsync([Remainder] string text)
        {
            // Reply
            var refMsg = Context.Message.ReferencedMessage;
            if (!string.IsNullOrWhiteSpace(refMsg?.Content))
            {
                var sb = new StringBuilder();

                var uwuifiedRefMsg = refMsg.Content.Uwuify();
                var uwuifiedText = text.Uwuify();

                sb.AppendLine(uwuifiedRefMsg.ToDiscordQuote());
                sb.Append(uwuifiedText);

                var outputMsg = sb.ToString();
                
                _logger.LogDebug("{commandName} result: {msg}", nameof(UwuAsync), outputMsg);
                
                await Context.Message.ReferencedMessage
                    .ReplyAsync(embed: outputMsg
                        .ToDefaultEmbed(Context, "Uwuify"));
            }
            // Normal invocation
            else
            {
                var outputMsg = text.Uwuify();
                _logger.LogDebug("{commandName} result: {msg}", nameof(UwuAsync), outputMsg);
                await Context.Message.ReplyAsync(embed: outputMsg
                    .ToDefaultEmbed(Context, "Uwuify"));
            }
        }

        // Uwuify someone else's message without your content
        [HiddenCommand("uwuify", RunMode = RunMode.Async)]
        [Alias("uwu", "owo")]
        public async Task UwuAsync()
        {
            if (string.IsNullOrWhiteSpace(Context.Message.ReferencedMessage?.Content)) return;
            var refMsg = Context.Message.ReferencedMessage;
            var outputMsg = refMsg.Content.Uwuify().ToDiscordQuote();
            _logger.LogDebug("{commandName} result: {msg}", nameof(UwuAsync), outputMsg);
            await Context.Message.ReferencedMessage.ReplyAsync(embed: outputMsg
                .ToDefaultEmbed(Context, "Uwuify"));
        }

        [Command("feedback", RunMode = RunMode.Async)]
        [Summary("Leave feedback for the developers on how you'd like this bot to work.")]
        public async Task FeedbackAsync([Remainder] string text)
        {
            _logger.LogInformation("New feedback left by {userName}. Feedback: {feedbackText}",
                Context.Message.Author.ToString(), text.Trim());

            await Context.Message.ReplyAsync(
                embed: "Thank you for your feedback! Your comments help the developers improve this bot."
                    .ToDefaultEmbed(Context, "Feedback Submitted"));
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("Print this, you big dummy!")]
        public async Task HelpAsync() =>
            await Context.Message.ReplyAsync(embed: (string.Join(Environment.NewLine, GetType()
                    .GetMethods()
                    .Where(m => m.GetCustomAttributes()
                        .All(a => a is not HiddenCommandAttribute))
                    .Select(m =>
                    {
                        var attributes = m.GetCustomAttributes();

                        var sb = new StringBuilder();

                        foreach (var attribute in attributes)
                        {
                            var text = attribute switch
                            {
                                CommandAttribute attr => $"Command: **{attr.Text}**; ",
                                AliasAttribute attr => $"Aliases: **{string.Join(", ", attr.Aliases)}**; ",
                                SummaryAttribute attr => $"Summary: *{attr.Text}*{Environment.NewLine}; ",
                                _ => string.Empty
                            };

                            sb.Append(text);
                        }

                        var output = sb.ToString();
                        return output.Length > 0 ? output[..^2] : output;
                    })) + "Note: Aliases are the same thing as Commands, just a faster way to use them!")
                .ToDefaultEmbed(Context, "Help"));
    }
}