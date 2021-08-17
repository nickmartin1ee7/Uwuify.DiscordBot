using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.Humanizer;

namespace Uwuify.DiscordBot.WorkerService.Modules
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Alias("latency")]
        [Summary("Returns the latency to Discord.")]
        public async Task PingAsync() =>
            await Context.Message.ReplyAsync(embed: $"{Context.Client.Latency} ms"
                .ToDefaultEmbed(Context, "Latency :ping_pong:"));

        [Command("echo")]
        [Summary("Repeats after you... Duh!")]
        public async Task EchoAsync([Remainder] string text) =>
            await Context.Message.ReplyAsync(embed: text
                .ToDefaultEmbed(Context, "Echo"));

        [Command("uwu")]
        [Alias("uwuify", "owo")]
        [Summary("Uwuify your message!")]
        public async Task UwuAsync([Remainder] string text)
        {
            // Reply
            if (!string.IsNullOrWhiteSpace(Context.Message.ReferencedMessage?.Content))
            {
                var refMsg = Context.Message.ReferencedMessage;
                var sb = new StringBuilder();

                sb.AppendLine($"> {refMsg.Content.Uwuify()}");
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

        [Command("uwu")]
        [Alias("uwuify", "owo")]
        [Summary("Uwuify someone else by replying to them!")]
        public async Task UwuAsync()
        {
            if (string.IsNullOrWhiteSpace(Context.Message.ReferencedMessage?.Content)) return;
            var refMsg = Context.Message.ReferencedMessage;
            await Context.Message.ReferencedMessage.ReplyAsync(embed: $"> {refMsg.Content.Uwuify()}"
                .ToDefaultEmbed(Context, "Uwuify"));
        }

        [Command("help")]
        [Summary("It prints this, you big dummy!")]
        public async Task HelpAsync() =>
            await Context.Message.ReplyAsync(embed: string.Join(Environment.NewLine, GetType()
                .GetMethods()
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
                            SummaryAttribute attr => $"Summary: {attr.Text}; ",
                            _ => string.Empty
                        };

                        sb.Append(text);
                    }

                    var output = sb.ToString();
                    return output.Length > 0 ? output[..^2] : output;
                })).ToDefaultEmbed(Context, "Help"));
    }
}