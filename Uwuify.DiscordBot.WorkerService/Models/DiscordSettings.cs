using System.Collections.Generic;

namespace Uwuify.DiscordBot.WorkerService.Models
{
    public class DiscordSettings
    {
        public string Token { get; set; }
        public string StatusMessage { get; set; }
        public IEnumerable<string> Prefixes { get; set; }
    }
}