using Discord.Commands;
using System.Threading.Tasks;
using Uwuify.Humanizer;

namespace Uwuify.DiscordBot.WorkerService.Modules
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        [Command("uwuify")]
        [Alias("uwu", "owo")]
        public Task UwuAsync([Remainder] string text) =>
            Task.FromResult(text.Uwuify());
    }
}
