using Uwuify.ShardManager.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var shardManager = new ShardManager(app.Configuration.GetValue<int>("ShardCount"));

app.MapGet("/requestId", shardManager.GetNextShard)
.WithName("GetRequestId");

app.Run();