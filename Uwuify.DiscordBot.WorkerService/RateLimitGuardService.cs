using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Remora.Rest.Core;

using Uwuify.DiscordBot.Data;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService;

public class RateLimitGuardService : IDisposable
{
    private readonly TimeSpan _slimTimeout = TimeSpan.FromSeconds(5);
    private readonly DiscordSettings _settings;
    private readonly DataContext _standardDataContext;
    private readonly DataContext _renewalDataContext;
    private readonly TaskCompletionSource _firstRenewalJobRun = new();
    private readonly SemaphoreSlim _standardContextSlim = new(1, 1);
    private Task _renewalJob;
    private CancellationTokenSource _renewalJobCts;

    public RateLimitGuardService(
        DiscordSettings settings,
        DataContext standardDataContext,
        DataContext renewalDataContext)
    {
        _settings = settings;
        _standardDataContext = standardDataContext;
        _renewalDataContext = renewalDataContext;

        Debug.Assert(_renewalDataContext != _standardDataContext);
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
            try
            {
                _renewalDataContext.ChangeTracker.Clear();
                var usageProfiles = await _renewalDataContext.RateLimitProfiles.ToArrayAsync();

                Dictionary<Snowflake, (RateLimitProfile RateLimitProfile, List<DateTime> UsageInUtc)> usageProfilesToModify = [];
                foreach (var usageProfile in usageProfiles)
                {
                    var snowflake = new Snowflake(usageProfile.Snowflake);

                    foreach (var usage in usageProfile.UsesInUtc)
                    {
                        var nextAvailableUsage = usage.Add(TimeSpan.FromMilliseconds(_settings.RateLimitingUsageFallOffInMilliSeconds));

                        // Required time has elapsed to remove usage (ie. falloff)
                        if (DateTime.UtcNow > nextAvailableUsage)
                        {
                            if (usageProfilesToModify.TryGetValue(snowflake, out var enqueuedModifications))
                            {
                                enqueuedModifications.UsageInUtc.Add(usage);
                            }
                            else
                            {
                                usageProfilesToModify.Add(snowflake, (usageProfile, new List<DateTime> { usage }));
                            }
                        }
                    }
                }

                foreach (var usageProfile in usageProfilesToModify)
                {
                    // Process old commands that have fallen off
                    foreach (var usageToRemove in usageProfile.Value.UsageInUtc)
                    {
                        var result = usageProfile.Value.RateLimitProfile.UsesInUtc.Remove(usageToRemove);
                        Debug.Assert(result);
                        Debug.WriteLine($"{usageProfile.Value.RateLimitProfile.Snowflake} usage at {usageToRemove} has fallen off.");
                    }

                    // Store changes
                    _renewalDataContext.Update(usageProfile.Value.RateLimitProfile);
                }

                await _renewalDataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Debugger.Break(); // This job needs to not ever fail
                throw;
            }
            finally
            {
                _firstRenewalJobRun.TrySetResult();
            }

            await Task.Delay(_settings.RateLimitingRenewalJobExecutionInMilliSeconds);
        }
    }

    public void StopRenewalJob()
    {
        _renewalJobCts?.Cancel();
    }

    public async Task<(bool IsRateLimited, DateTime? NextAvailableUsageInUtc)> IsRateLimited(Snowflake id)
    {
        DateTime? nextAvailableUsageInUtc = null;
        var now = DateTime.UtcNow;

        await _firstRenewalJobRun.Task;

        await _standardContextSlim.WaitAsync(_slimTimeout);

        var usageProfile = await GetRateLimitProfileBySnowflake(_standardDataContext, id);

        _standardContextSlim.Release();

        // Have they never made a rate-limited action before?
        if (usageProfile is null
            || usageProfile.CommandUses == 0)
        {
            Debug.WriteLine($"{id} was not rate limited.");
            return (false, nextAvailableUsageInUtc);
        }

        var oldestUsage = usageProfile.UsesInUtc.OrderBy(uses => uses).First();
        nextAvailableUsageInUtc = oldestUsage.Add(TimeSpan.FromMilliseconds(_settings.RateLimitingUsageFallOffInMilliSeconds));

        if (now <= nextAvailableUsageInUtc)
        {
            Debug.WriteLine($"{id} is rate limited until {nextAvailableUsageInUtc}.");
            return (true, nextAvailableUsageInUtc);
        }

        var isRateLimited = usageProfile.CommandUses >= _settings.RateLimitingMaxUsages;

        Debug.WriteLine($"{id} is" +
            $"{(isRateLimited
                ? $"rate limited until {nextAvailableUsageInUtc}"
                : "not rate limited")}" +
            $".");
        return (isRateLimited, nextAvailableUsageInUtc);
    }

    public async Task RecordUsage(Snowflake id)
    {
        await _firstRenewalJobRun.Task;

        await _standardContextSlim.WaitAsync(_slimTimeout);

        try
        {
            var usageProfile = await GetRateLimitProfileBySnowflake(_standardDataContext, id);

            if (usageProfile is null)
            {
                await _standardDataContext.RateLimitProfiles.AddAsync(new RateLimitProfile
                {
                    Snowflake = id.Value,
                    UsesInUtc = [DateTime.UtcNow]
                });

                return;
            }

            usageProfile.UsesInUtc.Add(DateTime.UtcNow);

            _standardDataContext.Update(usageProfile);
        }
        finally
        {
            await _standardDataContext.SaveChangesAsync();
        }

        _standardContextSlim.Release();
    }

    public void Dispose()
    {
        _renewalJobCts?.Cancel();
        _renewalJobCts?.Dispose();
        _renewalJob = null;
    }

    private static async Task<RateLimitProfile> GetRateLimitProfileBySnowflake(DataContext context, Snowflake snowflake)
    {
        context.ChangeTracker.Clear();
        return await context.RateLimitProfiles.FirstOrDefaultAsync(profile => profile.Snowflake == snowflake.Value);
    }
}
