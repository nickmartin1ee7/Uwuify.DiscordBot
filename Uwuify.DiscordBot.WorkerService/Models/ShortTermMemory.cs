using System;
using System.Collections.Generic;

using Remora.Rest.Core;

namespace Uwuify.DiscordBot.WorkerService.Models;

public static class ShortTermMemory
{
    public static HashSet<int> ShardsReady { get; } = new();
    public static HashSet<Snowflake> KnownGuilds { get; } = new();
    public static DateTime StartTime { get; set; } = DateTime.Now;
}