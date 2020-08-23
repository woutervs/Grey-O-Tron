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
        public TimedExecutionsService(TelemetryClient log,IDateTimeNowProvider dateTimeNowProvider , [NotNull] List<TimedExecution> actions)
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
            while (true)
            {
                var action = actions.FirstOrDefault(x => x.EnqueueTime <= dateTimeNowProvider.UtcNow);
                if (action != null && running)
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
                    var result = actions.Min(x => Math.Abs((x.EnqueueTime - dateTimeNowProvider.UtcNow).TotalMilliseconds));
                    var max = TimeSpan.FromSeconds(30).TotalMilliseconds;
                    await Task.Delay(TimeSpan.FromMilliseconds(result > max ? max : result));
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
