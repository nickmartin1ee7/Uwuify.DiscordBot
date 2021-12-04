using System.Collections.Generic;

namespace Uwuify.DiscordBot.WorkerService.Models
{
    public class DiscordSettings
    {
        public string StatusMessage { get; set; }
        public IEnumerable<string> Prefixes { get; set; }
        public ulong OwnerId { get; set; }
        public string DebugServerId { get; set; }
    }
}