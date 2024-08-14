using Remora.Rest.Core;

using Uwuify.DiscordBot.WorkerService;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.Tests;

public class RateLimitGuardServiceTests
{
    [Fact]
    public async Task IsRateLimitedAndStartRenewalJob_FalloffOldUsages()
    {
        // Arrange
        var delay = 1000;
        var userDelay = delay + 5;

        var service = new RateLimitGuardService(new DiscordSettings
        {
            RateLimitingRenewalJobExecutionInMilliSeconds = 1,
            RateLimitingUsageFallOffInMilliSeconds = delay,
            RateLimitingMaxUsages = 1
        });

        var timestamp = DateTimeOffset.Now;
        var snowflake = Snowflake.CreateTimestampSnowflake(timestamp);

        // Act
        service.StartRenewalJob(true);
        service.RecordUsage(snowflake);

        // Assert
        Assert.True(service.IsRateLimited(snowflake, out var nextAvailableUsage1), $"Should be rate limited! Next usage was: {nextAvailableUsage1}.");
        await Task.Delay(TimeSpan.FromMilliseconds(userDelay));
        Assert.False(service.IsRateLimited(snowflake, out var nextAvailableUsage2), $"Should not be rate limited! Previous usage was: {nextAvailableUsage1}; Next usage was: {nextAvailableUsage2}.");
    }
}