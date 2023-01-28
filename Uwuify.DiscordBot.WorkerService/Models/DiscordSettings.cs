using System;

namespace Uwuify.DiscordBot.WorkerService.Models;

public class DiscordSettings
{
    public ulong? DebugServerId { get; set; }
    public string Status { get; set; }
    public string Token { get; set; }
    public int Shards { get; set; } = 1;
    public string MetricsUri { get; set; }
    public string MetricsToken { get; set; }
    public string ProfanityWords { get; set; }
    public string[] ProfanityList => ProfanityWords?.Split(',') ?? Array.Empty<string>();
}