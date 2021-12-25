using Uwuify.ShardManager.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var originalMaxShards = app.Configuration
    .GetSection(nameof(ShardManager))
    .GetValue<int>("ShardCount");

var shardManager = new ShardManager(originalMaxShards);

app.MapGet("/requestShardGroup", (int groupSize) =>
    {
        try
        {
            return Results.Ok(shardManager.RequestShardGroup(groupSize));
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