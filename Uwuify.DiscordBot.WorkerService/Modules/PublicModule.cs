using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.DiscordBot.WorkerService.Models;
using Uwuify.Humanizer;

namespace Uwuify.DiscordBot.WorkerService.Modules
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
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

        [Command("uwuify", RunMode = RunMode.Async)]
        [Alias("uwu", "owo")]
        [Summary("Uwuify your message! Also, can Uwuify someone else by replying to them.")]
        public async Task UwuAsync([Remainder] string text)
        {
            // Reply
            if (!string.IsNullOrWhiteSpace(Context.Message.ReferencedMessage?.Content))
            {
                var refMsg = Context.Message.ReferencedMessage;
                var sb = new StringBuilder();

                sb.AppendLine(refMsg.Content.Uwuify().ToDiscordQuote());
                sb.Append(text.Uwuify());

                await Context.Message.ReferencedMessage
                    .ReplyAsync(embed: sb.ToString()
                        .ToDefaultEmbed(Context, "Uwuify"));
            }
            // Normal invocation
            else
            {
                await Context.Message.ReplyAsync(embed: text.Uwuify()
                    .ToDefaultEmbed(Context, "Uwuify"));
            }
        }

        [HiddenCommand("uwuify", RunMode = RunMode.Async)]
        [Alias("uwu", "owo")]
        public async Task UwuAsync()
        {
            if (string.IsNullOrWhiteSpace(Context.Message.ReferencedMessage?.Content)) return;
            var refMsg = Context.Message.ReferencedMessage;
            await Context.Message.ReferencedMessage.ReplyAsync(embed: refMsg.Content.Uwuify().ToDiscordQuote()
                .ToDefaultEmbed(Context, "Uwuify"));
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