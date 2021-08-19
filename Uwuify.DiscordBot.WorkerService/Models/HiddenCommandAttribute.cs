using Discord.Commands;

namespace Uwuify.DiscordBot.WorkerService.Models
{
    public class HiddenCommandAttribute : CommandAttribute
    {
        public HiddenCommandAttribute(string commandName)
            : base(commandName)
        {
        }
    }
}