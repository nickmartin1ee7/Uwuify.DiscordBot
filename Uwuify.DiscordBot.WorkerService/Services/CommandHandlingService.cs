﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Uwuify.DiscordBot.WorkerService.Models;

namespace Uwuify.DiscordBot.WorkerService.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _client;
        private readonly DiscordSettings _discordSettings;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _services;
        private readonly ILogger<CommandHandlingService> _logger;

        public CommandHandlingService(IServiceProvider services, ILogger<CommandHandlingService> logger, DiscordSocketClient client, DiscordSettings discordSettings, CommandService commandService)
        {
            _client = client;
            _discordSettings = discordSettings;
            _commandService = commandService;
            _services = services;
            _logger = logger;

            _client.MessageReceived += OnMessageReceivedAsync;
            _commandService.CommandExecuted += OnCommandExecutedAsync;
        }

        public async Task InitializeAsync()
        {
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _logger.LogDebug("Loaded commands: {commands}", _commandService.Commands.Select(c => c.Name));
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {

            if (result.IsSuccess)
            {
                _logger.LogInformation("{command} triggered by {user} successfully (#{channel} in {guild}).", command.IsSpecified ? command.Value.Name : "Command", context.Message.Author, context.Channel, context.Guild);
                return;
            }

            _logger.LogError("{command} errored when run by {user} (#{channel} in {guild}). Error: {error}", command.IsSpecified ? command.Value.Name : "Command", context.Message.Author, context.Channel, context.Guild, result);

            await context.Channel.SendMessageAsync($"That didn't work... {result}");
        }

        private async Task OnMessageReceivedAsync(SocketMessage arg)
        {
            if (arg is not SocketUserMessage socketMessage) return;
            if (socketMessage.Source != MessageSource.User) return;

            bool valid = false;

            int argPos = 0;

            valid |= socketMessage.HasMentionPrefix(_client.CurrentUser, ref argPos);
            valid |= _discordSettings.Prefixes.Any(prefix =>
                socketMessage.HasStringPrefix(prefix, ref argPos));

            if (!valid) return;

            var context = new SocketCommandContext(_client, socketMessage);

            await _commandService.ExecuteAsync(context, argPos, _services);
        }
    }
}
