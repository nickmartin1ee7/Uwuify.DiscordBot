using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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

        [HiddenCommand("eval", RunMode = RunMode.Async)]
        [Alias("evaluate", "e")]
        public async Task EvalAsync([Remainder] string input)
        {
            OwnerGuard.Validate(_ownerId, Context);

            using (Context.Message.Channel.EnterTypingState())
            {
                _logger.LogWarning("Admin is using Evaluate Command!");

                var script = input.Replace("```cs", string.Empty)
                    .Replace("```", string.Empty);

                var result = await _evaluationService.EvaluateAsync(script, Context);

                _logger.LogInformation("Eval returned: {result}", result);
                await Context.Message.ReplyAsync(
                    embed: $"`{result}`".ToDefaultEmbed(Context, "Output"));
            }
        }

        [HiddenCommand("status", RunMode = RunMode.Async)]
        public async Task StatusAsync()
        {
            OwnerGuard.Validate(_ownerId, Context);

            if (!Context.IsPrivate) return;

            var sb = new StringBuilder();

            sb.AppendLine("**Guild Info**");

            foreach (var guild in Context.Client.Guilds)
            {
                sb.Append($"{guild.Name}: {guild.MemberCount} users, ");
                sb.Append($"owned by {guild.Owner} ({guild.Owner?.Id.ToString() ?? "N/A"}), ");
                sb.Append($"{guild.Users.Count(u => u.IsBot)} bots, ");
                sb.Append($"{guild.Users.Count(u => u.GuildPermissions.Administrator)} admins, ");
                sb.Append(guild.CurrentUser.GuildPermissions.Administrator ? "**BOT IS ADMIN**" : string.Empty);
                sb.Append($"{guild.PreferredCulture.EnglishName} culture");
                sb.AppendLine(".");
            }

            await Context.Message.ReplyAsync(embed: sb.ToString()
                .ToDefaultEmbed(Context, "Status"));
        }
    }
}