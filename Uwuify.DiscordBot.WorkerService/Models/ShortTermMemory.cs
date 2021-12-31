using Remora.Rest.Core;
using System.Collections.Generic;

namespace Uwuify.DiscordBot.WorkerService.Models;

public static class ShortTermMemory
{
    public static HashSet<Snowflake> KnownGuilds { get; } = new();
}