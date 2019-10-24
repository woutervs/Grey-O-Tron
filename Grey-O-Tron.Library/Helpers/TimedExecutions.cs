using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.ApplicationInsights;

namespace GreyOTron.Library.Helpers
{
    public class TimedExecutions : IDisposable
    {
        private readonly TelemetryClient log;
        private CancellationTokenSource cancellationTokenSource;
        private readonly List<TimedExecution> actions = new List<TimedExecution>();
        public TimedExecutions(TelemetryClient log, BotMessages botMessages, VerifyAll verifyAll)
        {
            this.log = log;

            actions.Add(new TimedExecution
            {
                Name = "SetGameMessage",
                Action = async (d, c) => await botMessages.SetNextMessage(d, c),
                EnqueueTime = DateTime.UtcNow,
                NextOccurence = () => DateTime.UtcNow.Add(TimeSpan.FromSeconds(30))
            });
            actions.Add(new TimedExecution
            {
                Name = "VerifyAll",
                Action = async (d, c) => await verifyAll.Execute(d, c),
                EnqueueTime = DateTime.UtcNow,
                NextOccurence = () => DateTime.UtcNow.Date.Add(new TimeSpan(1, 22, 0, 0))
            });
        }

        public async Task Start(DiscordSocketClient client)
        {
            cancellationTokenSource = new CancellationTokenSource();
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var action = actions.FirstOrDefault(x => x.EnqueueTime <= DateTime.UtcNow);
                if (action != null)
                {
                    var nextOccurence = action.NextOccurence();
                    try
                    {
                        await action.Action(client, cancellationTokenSource.Token);
                        action.EnqueueTime = nextOccurence;
                    }
                    catch (Exception e)
                    {
                        log.TrackException(e, new Dictionary<string, string>
                        {
                            { "section", "TimedExecutions" },
                            { "action.name", action.Name },
                            { "action.enqueueTime", action.EnqueueTime.ToString(CultureInfo.InvariantCulture)},
                            { "action.nextOccurence", nextOccurence.ToString(CultureInfo.InvariantCulture) }
                        });
                    }
                }
                else
                {
                    var result = actions.Min(x => Math.Abs((x.EnqueueTime - DateTime.UtcNow).TotalMilliseconds));
                    await Task.Delay(TimeSpan.FromMilliseconds(result));
                }
            }
        }

        public async Task Stop()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            cancellationTokenSource?.Dispose();
        }
    }

    public class TimedExecution
    {
        public string Name { get; set; }
        public DateTime EnqueueTime { get; set; }
        public Func<DiscordSocketClient, CancellationToken, Task> Action { get; set; }

        public Func<DateTime> NextOccurence { get; set; }
    }
}
