using Microsoft.EntityFrameworkCore;

using Remora.Rest.Core;

using Uwuify.DiscordBot.Data;
using Uwuify.DiscordBot.WorkerService;
using Uwuify.DiscordBot.WorkerService.Models;

namespace TestProject1
{
    [TestClass]
    public sealed class RateLimitGuardServiceTests
    {
        [TestMethod]
        public async Task IsRateLimitedAndStartRenewalJob_FalloffOldUsages()
        {
            // Arrange
            var delay = 1000;
            var userDelay = delay + 5;

            var standardDataContext = new DataContext(new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: nameof(DataContext))
                .Options);

            var renewalDataContext = new DataContext(new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: nameof(DataContext))
                .Options);

            await standardDataContext.Database.EnsureCreatedAsync();
            await renewalDataContext.Database.EnsureCreatedAsync();

            var service = new RateLimitGuardService(
                new DiscordSettings
                {
                    RateLimitingRenewalJobExecutionInMilliSeconds = 1,
                    RateLimitingUsageFallOffInMilliSeconds = delay,
                    RateLimitingMaxUsages = 1
                },
                standardDataContext: standardDataContext,
                renewalDataContext: renewalDataContext);

            var timestamp = DateTime.UtcNow;
            var snowflake = Snowflake.CreateTimestampSnowflake(timestamp);

            // Act
            service.StartRenewalJob(startNewIfRunning: true);
            await service.RecordUsage(snowflake);
            var before = await service.IsRateLimited(snowflake);
            await Task.Delay(TimeSpan.FromMilliseconds(userDelay));
            var after = await service.IsRateLimited(snowflake);

            // Assert

            Assert.IsTrue(before.IsRateLimited, $"Should be rate limited! Next usage was: {before.NextAvailableUsageInUtc}.");
            Assert.IsFalse(after.IsRateLimited, $"Should not be rate limited! Previous usage was: {before.NextAvailableUsageInUtc}; Next usage was: {after.NextAvailableUsageInUtc}.");
        }
    }
}
