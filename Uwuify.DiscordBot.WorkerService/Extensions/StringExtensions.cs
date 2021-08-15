using Discord;
using Discord.Commands;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class StringExtensions
    {
        public static Embed ToDefaultEmbed(this string input, ICommandContext commandContext, string title)
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle(title)
                .WithDescription(input)
                .WithAuthor(commandContext.Message.Author)
                .WithCurrentTimestamp()
                .WithColor(new Color(0b_111110100110001110001010));
            
            return embedBuilder.Build();
        }
    }
}