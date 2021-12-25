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

app.MapPost("/unassignShardGroup", (int groupId) => shardManager.UnassignShardGroup(groupId))
    .WithName("PostResetShards");

app.MapPost("/unassignAllShardGroups", (int groupId) => shardManager.UnassignAllShardGroups())
    .WithName("PostUnassignAllShardGroups");

app.MapGet("/shardGroups", () => shardManager.GetShardGroups())
    .WithName("GetShardGroups");

app.MapGet("/maxShards", () => shardManager.GetMaxShards())
    .WithName("GetMaxShards");

app.MapPost("/maxShards", (int newShardCount) => shardManager.SetMaxShards(newShardCount))
    .WithName("SetMaxShards");

app.Run();