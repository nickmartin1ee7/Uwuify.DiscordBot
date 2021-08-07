﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uwuify.Humanizer;

namespace Uwuify.DiscordBot.WorkerService.Modules
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        [Command("echo")]
        [Summary("Repeats after you... Duh!")]
        public async Task EchoAsync([Remainder] string text) =>
            await ReplyAsync(text);

        [Command("uwuify")]
        [Alias("uwu", "owo")]
        [Summary("Formats text into it's UwU meme varient, of course!")]
        public async Task UwuAsync([Remainder] string text) =>
            await ReplyAsync(text.Uwuify());

        [Command("help")]
        [Summary("It prints this, you big dummy!")]
        public async Task HelpAsync() =>
            await ReplyAsync(string.Join(Environment.NewLine, GetType().GetMethods().Select(m =>
            {
                var cmds = new List<string>();
                var attributes = m.GetCustomAttributes();
                var sb = new StringBuilder();

                foreach (var attribute in attributes)
                {
                    var text = attribute switch
                    {
                        CommandAttribute attr => $"Command: **{attr.Text}**; ",
                        AliasAttribute attr => $"Aliases: **{string.Join(", ", attr.Aliases)}**; ",
                        SummaryAttribute attr => $"Summary: {attr.Text.Uwuify()}; ",
                        _ => string.Empty
                    };

                    sb.Append(text);
                }

                var output = sb.ToString();
                return output.Length > 0 ? output[..^2] : output;
            })));
    }
}