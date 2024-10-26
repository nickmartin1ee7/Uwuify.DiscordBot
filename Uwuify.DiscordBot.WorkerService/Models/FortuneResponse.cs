namespace Uwuify.DiscordBot.WorkerService.Models;

public class GenerateResponse
{
    public FortuneResponse fortune { get; set; }
    public double luck { get; set; }
    public string luckText { get; set; }
    public string context { get; set; }
}

public class FortuneResponse
{
    public string header { get; set; }
    public string body { get; set; }
}

