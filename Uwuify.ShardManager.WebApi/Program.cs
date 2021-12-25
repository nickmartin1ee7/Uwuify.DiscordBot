using Uwuify.ShardManager.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var maxShards = app.Configuration
    .GetSection(nameof(ShardManager))
    .GetValue<int>("MaxShards");

var internalShards = app.Configuration
    .GetSection(nameof(ShardManager))
    .GetValue<int>("InternalShards");

var shardManager = new ShardManager(maxShards, internalShards);

app.MapGet("/requestShardGroup", () =>
    {
        try
        {
            return Results.Ok(shardManager.RequestShardGroup());
        }
        catch (OutOfAvailableShardsException)
        {
            return Results.Conflict();
        }
    })
.WithName("GetRequestShardGroup");

app.MapGet("/shardGroups", () => shardManager.GetShardGroups())
    .WithName("GetShardGroups"); 

app.MapPost("/unassignShardGroup", (int groupId) => shardManager.UnassignShardGroup(groupId))
    .WithName("PostResetShards");

app.MapPost("/unassignAllShardGroups", () => shardManager.UnassignAllShardGroups())
    .WithName("PostUnassignAllShardGroups");

app.MapGet("/maxShards", () => shardManager.GetMaxShards())
    .WithName("GetMaxShards");

app.MapPost("/maxShards", (int newMaxShardCount) => shardManager.SetMaxShards(newMaxShardCount))
    .WithName("SetMaxShards");

app.MapGet("/internalShards", () => shardManager.GetInternalShards())
    .WithName("GetInternalShards");

app.MapPost("/internalShards", (int newInternalShardCount) => shardManager.SetInternalShards(newInternalShardCount))
    .WithName("SetInternalShards");

app.Run();