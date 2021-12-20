using System.Collections.Generic;
using Remora.Rest.Core;

namespace Uwuify.DiscordBot.WorkerService.Models
{
    public static class InMemoryGuildStorage
    {
        public static HashSet<Snowflake> Guilds { get; } = new();
    }
}
