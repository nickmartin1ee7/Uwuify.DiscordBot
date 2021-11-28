using System.Text;
using Discord;
using Discord.Commands;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class StringExtensions
    {
        private static readonly Color _pinkColor = new Color(0b_111110100110001110001010);

        public static Embed ToDefaultEmbed(this string input, ICommandContext commandContext, string title, string imageUrl = null)
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle(title)
                .WithDescription(input)
                .WithAuthor(commandContext.Message.Author)
                .WithCurrentTimestamp()
                .WithImageUrl(imageUrl)
                .WithColor(_pinkColor);

            return embedBuilder.Build();
        }

        public static string ToDiscordQuote(this string input)
        {
            var sb = new StringBuilder();
            var lines = input.Split('\n');
            lines.ForEach(l => sb.AppendLine("> " + l));
            return sb.ToString();
        }

        public static bool ShouldWarningLogMessage(this LogMessage logMessage) =>
            !string.IsNullOrEmpty(logMessage.Message)
            && !logMessage.Message.Contains("[null]") // Null warning messages from Discord
            && !logMessage.Message.Contains("Unknown Dispatch"); // Unregistered event handlers for client

        public static bool ShouldInfoLogMessage(this LogMessage logMessage) =>
            !string.IsNullOrEmpty(logMessage.Message)
            && !logMessage.Message.StartsWith("Left ") // Leaving a guild is custom
            && !logMessage.Message.StartsWith("Joined "); // Joining a guild is custom
    }
}