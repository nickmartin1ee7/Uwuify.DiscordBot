using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using static Remora.Discord.API.Abstractions.Gateway.Events.IGuildCreate;

namespace Uwuify.DiscordBot.WorkerService.Models;

public static class ShortTermMemory
{
    private static int _knownUserCount = 0;

    public static DateTime StartTime { get; set; } = DateTime.Now;
    public static HashSet<int> ShardsReady { get; } = [];
    public static int KnownUserCount => _knownUserCount;
    public static HashSet<Snowflake> KnownGuilds { get; } = [];

    public static void AddKnownGuild(IUnavailableGuild guild)
    {
        KnownGuilds.Add(guild.ID);
    }

    public static void AddKnownGuild(IAvailableGuild guild)
    {
        KnownGuilds.Add(guild.ID);
        Interlocked.Add(ref _knownUserCount, guild.MemberCount);
    }
}