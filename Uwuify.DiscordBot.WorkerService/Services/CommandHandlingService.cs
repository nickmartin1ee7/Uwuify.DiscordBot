using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Uwuify.DiscordBot.WorkerService.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _services;
        private readonly ILogger<CommandHandlingService> _logger;

        public CommandHandlingService(IServiceProvider services, ILogger<CommandHandlingService> logger, DiscordSocketClient client, CommandService commandService)
        {
            _client = client;
            _commandService = commandService;
            _services = services;
            _logger = logger;

            _client.MessageReceived += OnMessageReceivedAsync;
            _commandService.CommandExecuted += OnCommandExecutedAsync;
        }

        public async Task InitializeAsync() => await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified || result.IsSuccess)
                return;

            _logger.LogError("Command execution failed.", result, command);

            await context.Channel.SendMessageAsync($"That didn't work... {result}");
        }

        private async Task OnMessageReceivedAsync(SocketMessage arg)
        {
            if (arg is not SocketUserMessage socketMessage) return;
            if (socketMessage.Source != MessageSource.User) return;

            var argPos = 0;
            if (!socketMessage.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;

            var context = new SocketCommandContext(_client, socketMessage);

            await _commandService.ExecuteAsync(context, argPos, _services);
        }
    }
}
