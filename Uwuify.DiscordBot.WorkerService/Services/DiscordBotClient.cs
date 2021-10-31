using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uwuify.DiscordBot.WorkerService.Extensions;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService.Services
{
    public class DiscordBotClient
    {
        private readonly ILogger<DiscordBotClient> _logger;
        private readonly DiscordSettings _discordSettings;
        private readonly CommandHandlingService _commandHandlingService;
        private readonly DiscordSocketClient _client;
        private readonly System.Timers.Timer _statusTimer;

        public DiscordBotClient(ILogger<DiscordBotClient> logger,
            DiscordSocketClient client,
            DiscordSettings discordSettings,
            CommandHandlingService commandHandlingService)
        {
            _logger = logger;
            _discordSettings = discordSettings;
            _commandHandlingService = commandHandlingService;
            _client = client;

            _statusTimer = new System.Timers.Timer(TimeSpan.FromSeconds(60).TotalMilliseconds);
            _statusTimer.Elapsed += async (s, e) =>
            {
                await _client.SetGameAsync(_discordSettings.StatusMessage, type: ActivityType.Playing);
            };
        }

        public async Task Run(CancellationToken stoppingToken)
        {
            RegisterEventHandlers();
            await StartClientAsync();
            await SetInitialActivityAsync();
            _statusTimer.Start();

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task SetInitialActivityAsync()
        {
            await _client.SetGameAsync(_discordSettings.StatusMessage, type: ActivityType.Playing);
            _logger.LogInformation("Game Status set to {msg}", _discordSettings.StatusMessage);
        }

        private async Task StartClientAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _discordSettings.Token);
            await _client.StartAsync();
            await _commandHandlingService.InitializeAsync();
        }

        private void RegisterEventHandlers()
        {
            _client.Log += OnLogAsync;
            _client.Ready += OnReadyAsync;
            _client.JoinedGuild += OnGuildJoinAsync;
            _client.LeftGuild += OnGuildLeftAsync;
        }

        private async Task OnGuildJoinAsync(SocketGuild arg)
        {
            _logger.LogInformation("Joined new guild: {guildName} ({guildId})", arg.Name, arg.Id);
            LogGuildCount();
        }

        private async Task OnGuildLeftAsync(SocketGuild arg)
        {
            _logger.LogInformation("Left guild: {guildName} ({guildId})", arg.Name, arg.Id);
            LogGuildCount();
        }

        private void LogGuildCount()
        {
            _logger.LogInformation("Guild count: {count}", _client.Guilds.Count);
        }

        private Task OnLogAsync(LogMessage logMessage)
        {
            switch (logMessage.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(logMessage.Message, logMessage);
                    break;
                case LogSeverity.Error:
                    _logger.LogError(logMessage.Message, logMessage);
                    break;
                case LogSeverity.Warning:
                    if (logMessage.ShouldWarningLogMessage())
                        _logger.LogWarning(logMessage.Message, logMessage);
                    break;
                case LogSeverity.Info:
                    if (logMessage.ShouldInfoLogMessage())
                        _logger.LogInformation(logMessage.Message, logMessage);
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace(logMessage.Message, logMessage);
                    break;
                case LogSeverity.Debug:
                    _logger.LogDebug(logMessage.Message, logMessage);
                    break;
                default:
                    _logger.LogError("Log level not supported!", logMessage);
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task OnReadyAsync()
        {
            var guilds = _client.Guilds.ToSingleString();

            _logger.LogInformation("{botUser} is online for {guildCount} guilds: {guilds}", _client.CurrentUser,
                _client.Guilds.Count, guilds);
        }
    }
}