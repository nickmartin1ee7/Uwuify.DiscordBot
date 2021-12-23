using Uwuify.ShardManager.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var shardManager = new ShardManager(app.Configuration.GetValue<int>("ShardCount"));

app.MapGet("/requestId", () =>
    {
        try
        {
            return Results.Ok(shardManager.GetNextShard());
        }
        catch (OutOfAvailableShardsException)
        {
            return Results.Conflict();
        }
        catch (ShardingNotAllowedException)
        {
            return Results.BadRequest();
        }
    })
.WithName("GetRequestId");

app.MapGet("/currentShards", shardManager.GetCurrentShards)
    .WithName("GetCurrentShards");

app.MapGet("/currentShardCount", shardManager.GetCurrentShardCount)
    .WithName("GetCurrentShardCount");

app.MapPost("/growShardCount", (int shardCount) => shardManager.GrowShardCount(shardCount))
    .WithName("PostGrowShardCount");

app.MapPost("/resetShards", () => shardManager.ResetShards())
    .WithName("PostResetShards");

app.Run();