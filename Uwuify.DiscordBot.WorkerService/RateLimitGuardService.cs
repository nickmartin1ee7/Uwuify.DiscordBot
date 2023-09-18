using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Remora.Rest.Core;

using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService;

public class RateLimitGuardService : IDisposable
{
    private readonly DiscordSettings _settings;
    private readonly ConcurrentDictionary<Snowflake, RateLimitProfile> _usageDict = new();

    private Task _renewalJob;
    private CancellationTokenSource _renewalJobCts;

    public RateLimitGuardService(DiscordSettings settings)
    {
        _settings = settings;
    }

    public void StartRenewalJob(bool startNewIfRunning = false)
    {
        if (_renewalJobCts != null
            && !_renewalJobCts.IsCancellationRequested
            && !startNewIfRunning)
            return;

        _renewalJobCts?.Cancel();
        _renewalJobCts = new CancellationTokenSource();

        _renewalJob = Task.Run(RenewalJob);
    }

    private async Task RenewalJob()
    {
        while (!_renewalJobCts.IsCancellationRequested)
        {
            var usageProfilesToModify = new Dictionary<Snowflake, (RateLimitProfile, List<DateTimeOffset>)>();
            foreach (var usageProfile in _usageDict)
            {
                foreach (var usage in usageProfile.Value.Usages)
                {
                    var spanSinceUsage = DateTimeOffset.Now - usage;

                    if (spanSinceUsage > TimeSpan.FromMilliseconds(_settings.RateLimitingUsageFallOffInMilliSeconds))
                    {
                        if (usageProfilesToModify.TryGetValue(usageProfile.Key, out var enqueuedModifications))
                        {
                            enqueuedModifications.Item2.Add(usage);
                        }
                        else
                        {
                            usageProfilesToModify.Add(usageProfile.Key, (usageProfile.Value, new List<DateTimeOffset> { usage }));
                        }
                    }
                }
            }

            foreach (var usageProfile in usageProfilesToModify)
            {
                foreach (var usageToRemove in usageProfile.Value.Item2)
                {
                    _usageDict[usageProfile.Key].Usages.Remove(usageToRemove);
                }
            }

            await Task.Delay(_settings.RateLimitingRenewalJobExecutionInMilliSeconds);
        }
    }

    public void StopRenewalJob()
    {
        _renewalJobCts?.Cancel();
    }

    public bool IsRateLimited(Snowflake id, out DateTimeOffset? nextAvailableUsage)
    {
        nextAvailableUsage = null;

        if (!_usageDict.TryGetValue(id, out var usageProfile)
            || !usageProfile.Usages.Any())
            return false;

        var oldestUsage = usageProfile.Usages.OrderBy(up => up).First();
        var spanSinceUsage = DateTimeOffset.Now - oldestUsage;

        nextAvailableUsage = DateTimeOffset.Now.Add(spanSinceUsage.Add(TimeSpan.FromMilliseconds(_settings.RateLimitingUsageFallOffInMilliSeconds)));

        return usageProfile.TotalUsage >= _settings.RateLimitingMaxUsages;
    }

    public void RecordUsage(Snowflake id)
    {
        if (!_usageDict.TryGetValue(id, out var usageProfile))
        {
            _usageDict.TryAdd(id, new RateLimitProfile
            {
                Usages = new List<DateTimeOffset> { DateTimeOffset.Now }
            });

            return;
        }

        usageProfile.Usages.Add(DateTimeOffset.Now);
    }

    public void Dispose()
    {
        _renewalJobCts.Cancel();
        _renewalJob.Dispose();
    }
}

public class RateLimitProfile
{
    public int TotalUsage => Usages.Count;
    public List<DateTimeOffset> Usages { get; set; } = new List<DateTimeOffset>();
}
