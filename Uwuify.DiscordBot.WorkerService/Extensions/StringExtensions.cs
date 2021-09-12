using System.Text;
using Discord;
using Discord.Commands;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class StringExtensions
    {
        private static readonly Color _pinkColor = new Color(0b_111110100110001110001010);

        public static Embed ToDefaultEmbed(this string input, ICommandContext commandContext, string title)
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle(title)
                .WithDescription(input)
                .WithAuthor(commandContext.Message.Author)
                .WithCurrentTimestamp()
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

        public static bool ShouldIgnoreWarningLogMessage(this LogMessage logMessage) =>
            !string.IsNullOrEmpty(logMessage.Message)
            && !logMessage.Message.Contains("[null]")
            && !logMessage.Message.Contains("Unknown Dispatch");
    }
}