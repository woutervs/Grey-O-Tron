using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace GreyOTron.Library.Models
{
    public class TimedExecution
    {
        public string Name { get; set; }
        public DateTime EnqueueTime { get; set; }
        public Func<IDiscordClient, CancellationToken, Task> Action { get; set; }
        public Func<DateTime> NextOccurence { get; set; }
    }
}