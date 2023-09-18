namespace Uwuify.DiscordBot.WorkerService.Models;


public class FortuneResponse
{
    public int MaxTokens { get; set; }
    public float CostLimit { get; set; }
    public string Context { get; set; }
    public string Fortune { get; set; }
}

