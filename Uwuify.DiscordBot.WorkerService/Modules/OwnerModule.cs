using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.DiscordBot.WorkerService.Guards;
using Uwuify.DiscordBot.WorkerService.Models;
using Uwuify.DiscordBot.WorkerService.Services;

namespace Uwuify.DiscordBot.WorkerService.Modules
{
    public class OwnerModule : ModuleBase<SocketCommandContext>
    {
        // Can't DI since Discord library uses reflection
        private readonly ILogger<OwnerModule> _logger = Program.Services.GetService<ILogger<OwnerModule>>();
        private readonly EvaluationService _evaluationService = Program.Services.GetService<EvaluationService>();
        private readonly ulong _ownerId = Program.Services.GetService<DiscordSettings>().OwnerId;

        [HiddenCommand("shutdown")]
        public async Task ShutdownAsync()
        {
            OwnerGuard.Validate(_ownerId, Context);

            _logger.LogWarning("{user} ({userId}) called for a shutdown", Context.Message.Author,
                Context.Message.Author.Id);

            Environment.Exit(Context.Message.Author.Id.GetHashCode());
        }

        [HiddenCommand("eval", RunMode = RunMode.Async)]
        [Alias("evaluate", "e")]
        public async Task EvalAsync([Remainder] string input)
        {
            OwnerGuard.Validate(_ownerId, Context);

            using (Context.Message.Channel.EnterTypingState())
            {
                var script = input.Replace("```cs", string.Empty)
                    .Replace("```", string.Empty);

                var result = await _evaluationService.EvaluateAsync(script, Context);

                _logger.LogInformation("Eval returned: {result}", result);
                await Context.Message.ReplyAsync(
                    embed: $"`{result}`".ToDefaultEmbed(Context, "Output"));
            }
        }

        [HiddenCommand("send", RunMode = RunMode.Async)]
        public async Task SendAsync([Remainder] string input)
        {
            OwnerGuard.Validate(_ownerId, Context);

            const char DELIM = '|';
            var inputs = input.Split(DELIM);

            if (inputs.Length < 2)
            {
                await Context.Message.ReplyAsync(
                    $"Invalid usage. Guild ID or Name and the message (delimited by {DELIM}).");
                return;
            }

            SocketGuild guild = ulong.TryParse(inputs[0], out var guildId)
                ? Context.Client.Guilds.FirstOrDefault(g => g.Id == guildId)
                : Context.Client.Guilds.FirstOrDefault(g => g.Name.Contains(inputs[0]));

            if (guild is null)
            {
                await Context.Message.ReplyAsync($"Guild {inputs[0]} could not be found.");
                return;
            }

            _logger.LogInformation("Sending message to guild {guild}: {message}", guild, input);

            _ = await guild.DefaultChannel.SendMessageAsync(inputs[1]);
        }

        [HiddenCommand("delete", RunMode = RunMode.Async)]
        public async Task DeleteAsync([Remainder] string input)
        {
            OwnerGuard.Validate(_ownerId, Context);

            var messageId = ulong.Parse(input);

            _logger.LogInformation("Deleting message {messageId}", messageId);

            await Context.Channel.DeleteMessageAsync(messageId);
        }

        [HiddenCommand("announcement", RunMode = RunMode.Async)]
        [Alias("announce", "broadcast")]
        public async Task AnnouncementAsync([Remainder] string input)
        {
            OwnerGuard.Validate(_ownerId, Context);

            _logger.LogInformation("Announcement being sent to {guildCount} guilds: {msg}", Context.Client.Guilds.Count,
                input);

            Context.Client.Guilds.ForEach(async guild =>
                await guild.DefaultChannel.SendMessageAsync(embed: input.ToDefaultEmbed(Context, "Announcement")));
        }

        [HiddenCommand("status", RunMode = RunMode.Async)]
        public async Task StatusAsync()
        {
            OwnerGuard.Validate(_ownerId, Context);

            if (!Context.IsPrivate)
                return;

            var sb = new StringBuilder();

            sb.AppendLine($"**Guild Info: {Context.Client.Guilds.Count}**");

            foreach (var guild in Context.Client.Guilds)
            {
                sb.Append($"{guild.Name} ({guild.Id}): {guild.MemberCount} users, ");
                sb.Append($"owned by {guild.Owner} ({guild.Owner?.Id.ToString() ?? "N/A"}), ");
                sb.Append($"{guild.Users.Count(u => u.IsBot)} bots, ");
                sb.Append(guild.CurrentUser.GuildPermissions.Administrator ? "**BOT IS ADMIN**; " : string.Empty);
                sb.Append($"{guild.PreferredCulture.EnglishName}");
                sb.AppendLine(".");
            }

            var result = sb.ToString();
            const int MAX_LEN = 2000;

            if (result.Length > MAX_LEN)
            {
                int parts = (int)Math.Ceiling(result.Length / (double)MAX_LEN); // Amount of msgs needed
                var messageParts = new string[parts];

                int lastPos = 0;
                for (int i = 0; i < parts; i++)
                {
                    var pos = lastPos + MAX_LEN;
                    if (pos >= result.Length) pos = result.Length;
                    messageParts[i] = result[lastPos..pos];
                    lastPos = pos;
                }

                foreach (var part in messageParts)
                {
                    await Context.Message.ReplyAsync(part);
                    await Task.Delay(800);
                }
            }
            else
            {
                await Context.Message.ReplyAsync(result);
            }
        }
    }
}