namespace Uwuify.DiscordBot.WorkerService.Models;

public class DiscordSettings
{
    public ulong? OwnerId { get; set; }
    public ulong? DebugServerId { get; set; }
    public string Status { get; set; }
    public int? ShardId { get; set; }
    public int? ShardCount { get; set; }
}