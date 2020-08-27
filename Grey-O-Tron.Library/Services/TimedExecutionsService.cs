using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Models;
using Microsoft.ApplicationInsights;

namespace GreyOTron.Library.Services
{
    public class TimedExecutionsService : IDisposable
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly TelemetryClient log;
        private readonly IDateTimeNowProvider dateTimeNowProvider;
        private readonly List<TimedExecution> actions;
        private bool running;
        public TimedExecutionsService(TelemetryClient log, IDateTimeNowProvider dateTimeNowProvider, [NotNull] List<TimedExecution> actions)
        {
            this.log = log;
            this.dateTimeNowProvider = dateTimeNowProvider;
            this.actions = actions;
        }

        public async Task Start()
        {
            running = true;
            cancellationTokenSource = new CancellationTokenSource();
            await Task.CompletedTask;
        }

        public async Task Setup(IDiscordClient client)
        {
            var epsilon = TimeSpan.FromSeconds(30).TotalMilliseconds;
            while (true)
            {
                var current = dateTimeNowProvider.UtcNow;
                actions.Where(x => x.EnqueueTime < current.AddMilliseconds(-epsilon / 2)).ToList().ForEach(x => x.EnqueueTime = x.NextOccurence());
                var actionsToExecute = actions.Where(x => x.EnqueueTime >= current.AddMilliseconds(-epsilon / 2) && x.EnqueueTime <= current.AddMilliseconds(epsilon / 2)).ToList();
                if (actionsToExecute.Any() && running)
                {
                    var tasks = actionsToExecute.Select(x =>
                    {
                        var nextOccurence = x.NextOccurence();
                        return Task.Run(async () =>
                        {
                            try
                            {
                                await x.Action(client, cancellationTokenSource.Token);
                            }
                            catch (Exception e)
                            {
                                log.TrackException(e, new Dictionary<string, string>
                            {
                                    { "section", "TimedExecutions" },
                                    { "action.name", x.Name },
                                    { "action.enqueueTime", x.EnqueueTime.ToString(CultureInfo.InvariantCulture)},
                                    { "action.nextOccurence", nextOccurence.ToString(CultureInfo.InvariantCulture) }
                            });
                            }
                            x.EnqueueTime = nextOccurence;
                        });
                    });
                    await Task.WhenAll(tasks);
                }
                else
                {
                    var result = actions.Min(x => Math.Abs((x.EnqueueTime - dateTimeNowProvider.UtcNow).TotalMilliseconds));
                    await Task.Delay(TimeSpan.FromMilliseconds(result > epsilon ? epsilon : result));
                }
            }
        }

        public async Task Stop()
        {
            running = false;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            cancellationTokenSource?.Dispose();
        }
    }
}
