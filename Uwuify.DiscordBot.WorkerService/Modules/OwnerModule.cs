using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uwuify.DiscordBot.WorkerService.Extensions;
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

        [HiddenCommand]
        [Command("eval")]
        [Alias("evaluate", "e")]
        public async Task EvalAsync([Remainder] string input)
        {
            if (!Context.Message.Author.Id.Equals(_ownerId)) return;

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
    }
}