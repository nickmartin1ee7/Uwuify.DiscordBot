using System;
using Discord.Commands;

namespace Uwuify.DiscordBot.WorkerService.Guards
{
    public static class OwnerGuard
    {
        public static void Validate(ulong ownerId, SocketCommandContext context)
        {
            if (!context.Message.Author.Id.Equals(ownerId))
                throw new InvalidOperationException(
                    $"{context.Message.Author} is not allowed to perform this command.");
        }
    }
}