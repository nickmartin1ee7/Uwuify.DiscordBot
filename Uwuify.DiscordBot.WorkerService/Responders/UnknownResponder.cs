using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Uwuify.DiscordBot.WorkerService.Responders;

public class UnknownResponder : IResponder<IUnknownEvent>
{
    private readonly ILogger<UnknownResponder> _logger;

    public UnknownResponder(ILogger<UnknownResponder> logger)
    {
        _logger = logger;
    }

    public async Task<Result> RespondAsync(IUnknownEvent gatewayEvent, CancellationToken ct = new())
    {
        _logger.LogTrace("Unknown Event from Gateway: {unknownEvent}", JsonSerializer.Serialize(gatewayEvent));

        return Result.FromSuccess();
    }
}