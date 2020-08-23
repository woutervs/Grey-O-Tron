using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using FakeItEasy;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Models;
using GreyOTron.Library.Services;
using Microsoft.ApplicationInsights;
using Xunit;
using Xunit.Abstractions;

namespace GreyOTron.Library.Tests
{
    public class TimedExecutionsTests
    {
        private readonly ITestOutputHelper outputHelper;

        public TimedExecutionsTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Theory]
        [GreyOTronLibraryAutoData]
        public async Task Test_TimedExecutions(IDiscordClient client, IDateTimeNowProvider dateTimeNowProvider)
        {
            A.CallTo(() => dateTimeNowProvider.UtcNow).ReturnsNextFromSequence(new DateTime(2020, 8, 1, 20, 0, 0),
                new DateTime(2020, 8, 1, 21, 0, 0),
                new DateTime(2020, 8, 2, 20, 0, 0));

            var timedExecutionService = new TimedExecutionsService(new TelemetryClient(), dateTimeNowProvider, new List<TimedExecution>
            {
                new TimedExecution
                {
                    Name = "Test",
                    Action = (a,  b) =>
                    {
                        outputHelper.WriteLine($"Executed Test");
                        return Task.CompletedTask;
                    },
                    EnqueueTime = new DateTime(2020, 8, 1, 19, 50, 0),
                    NextOccurence = () =>
                    {
                        var next = dateTimeNowProvider.UtcNow.Date.Add(new TimeSpan(1, 20, 0, 0));
                        outputHelper.WriteLine($"Next verifyAll: {next}");
                        return next;
                    }

                }
            });

            await timedExecutionService.Start();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await timedExecutionService.Setup(client)).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await Task.Delay(5000);

            await timedExecutionService.Stop();

        }
    }
}
