using System.Text.Json;
using System.Text.Json.Serialization;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Rest.Core;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class UnknownEventExtensions
    {
        public static bool TryHandleAsGuildDelete(this IUnknownEvent unknownEvent, out Snowflake guildId)
        {
            guildId = default;

            var gatewayEvent = JsonSerializer.Deserialize<CustomUnknownGatewayEvent>(unknownEvent.Data);

            if (gatewayEvent?.GatewayType == "GUILD_DELETE")
            {
                guildId = new Snowflake(ulong.Parse(gatewayEvent.UnavailableGuild.Id));
                return true;
            }

            return false;
        }

        internal class UnavailableGuild
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
        }

        internal class CustomUnknownGatewayEvent
        {
            [JsonPropertyName("t")]
            public string GatewayType { get; set; }

            [JsonPropertyName("d")]
            public UnavailableGuild UnavailableGuild { get; set; }
        }
    }
}
