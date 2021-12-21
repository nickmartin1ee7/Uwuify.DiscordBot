using System.Collections.Generic;
using Remora.Rest.Core;

namespace Uwuify.DiscordBot.WorkerService.Models;

public static class ShortTermMemory
{
    public static HashSet<Snowflake> KnownGuilds { get; } = new();
}