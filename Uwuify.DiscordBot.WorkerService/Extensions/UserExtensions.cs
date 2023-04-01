using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class UserExtensions
    {
        public static string ToFullUsername(this IUser user) => $"{user.Username}#{user.Discriminator}";

        public static IUser TryGetUser(this ICommandContext ctx)
        {
            switch (ctx)
            {
                case IInteractionContext interactionCommandContext:
                {
                    if (interactionCommandContext.Interaction.User.TryGet(out var user))
                    {
                        return user;
                    }

                    if (interactionCommandContext.Interaction.Member.TryGet(out var member))
                    {
                        if (member.User.TryGet(out user))
                        {
                            return user;
                        }
                    }

                    break;
                }
                case IMessageContext { Message.Author.HasValue: true } messageContext:
                {
                    return messageContext.Message.Author.Value;
                }
            }

            return null;
        }
    }
}
