using System;
using System.Collections.Generic;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Models;
using GreyOTron.Library.Services;
using Microsoft.ApplicationInsights;

namespace GreyOTron.Library.Helpers
{
    public class TimedExecutionsHelper
    {
        public List<TimedExecution> Actions { get; set; } = new List<TimedExecution>();
        public TimedExecutionsHelper(TelemetryClient log, CarouselMessagesService botMessages, VerifyAllHelper verifyAll, IDateTimeNowProvider dateTimeNowProvider, IEnvironmentHelper environmentHelper)
        {
            Actions.Add(new TimedExecution
            {
                Name = "SetGameMessage",
                Action = async (d, c) => await botMessages.SetNextMessage(d, c),
                EnqueueTime = dateTimeNowProvider.UtcNow,
                NextOccurence = () => dateTimeNowProvider.UtcNow.Add(TimeSpan.FromSeconds(30))
            });
            if (environmentHelper.Is(Environments.Production))
            {
                Actions.Add(new TimedExecution
                {
                    Name = "VerifyAll",
                    Action = async (d, c) => await verifyAll.Execute(d, c),
                    EnqueueTime = dateTimeNowProvider.UtcNow.Date.Add(new TimeSpan(0, 20, 0, 0)),
                    NextOccurence = () =>
                    {
                        var next = dateTimeNowProvider.UtcNow.Date.Add(new TimeSpan(1, 20, 0, 0));
                        log.TrackTrace($"Next verifyAll: {next}");
                        return next;
                    }
                });
            }
        }
    }
}
