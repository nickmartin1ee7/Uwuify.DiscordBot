using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class GuildsExtensions
    {
        public static string ToSingleString(this IReadOnlyCollection<SocketGuild> guilds)
        {
            var sb = new StringBuilder();

            guilds.ForEach(guild => sb.Append($"{guild.Name} ({guild.Id}); "));

            return sb.ToString().Trim();
        }
    }
}