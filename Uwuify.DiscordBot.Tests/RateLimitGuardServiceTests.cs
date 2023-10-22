using Remora.Rest.Core;

using Uwuify.DiscordBot.WorkerService;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.Tests
{
    public class RateLimitGuardServiceTests
    {
        [Fact]
        public async void IsRateLimitedAndStartRenewalJob_FalloffOldUsages()
        {
            // Arrange
            var delay = 50;
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
            Assert.True(service.IsRateLimited(snowflake, out var nextAvailableUsage1));
            await Task.Delay(TimeSpan.FromMilliseconds(delay));
            Assert.False(service.IsRateLimited(snowflake, out var nextAvailableUsage2));
        }
    }
}