using System;
using System.Collections.Generic;

namespace Uwuify.DiscordBot.WorkerService.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, params Action<T>[] actions)
        {
            foreach (var item in enumerable)
            foreach (var action in actions)
                action(item);
        }
    }
}