using Microsoft.EntityFrameworkCore;

using Remora.Rest.Core;

using Uwuify.DiscordBot.Data;
using Uwuify.DiscordBot.WorkerService;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.Tests;

[TestFixture]
public class RateLimitGuardServiceTests
{
    [Test]
    public async Task IsRateLimitedAndStartRenewalJob_FalloffOldUsages()
    {
        // Arrange
        var delay = 1000;
        var userDelay = delay + 5;

        var dataContext = new DataContext(new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: nameof(DataContext))
            .Options);

        await dataContext.Database.EnsureCreatedAsync();
        await dataContext.Database.MigrateAsync();

        var service = new RateLimitGuardService(
            new DiscordSettings
            {
                RateLimitingRenewalJobExecutionInMilliSeconds = 1,
                RateLimitingUsageFallOffInMilliSeconds = delay,
                RateLimitingMaxUsages = 1
            },
            standardDataContext: dataContext,
            renewalDataContext: dataContext);

        var timestamp = DateTime.UtcNow;
        var snowflake = Snowflake.CreateTimestampSnowflake(timestamp);

        // Act
        service.StartRenewalJob(true);
        await service.RecordUsage(snowflake);
        var a = await service.IsRateLimited(snowflake);
        var b = await service.IsRateLimited(snowflake);

        // Assert

        Assert.That(a.IsRateLimited, $"Should be rate limited! Next usage was: {a.NextAvailableUsageInUtc}.");
        await Task.Delay(TimeSpan.FromMilliseconds(userDelay));
        Assert.That(!b.IsRateLimited, $"Should not be rate limited! Previous usage was: {a.NextAvailableUsageInUtc}; Next usage was: {b.NextAvailableUsageInUtc}.");
    }
}