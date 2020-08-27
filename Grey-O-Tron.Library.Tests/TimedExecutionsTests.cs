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
            //A.CallTo(() => dateTimeNowProvider.UtcNow).ReturnsNextFromSequence(new DateTime(2020, 8, 1, 20, 0, 0),
            //    new DateTime(2020, 8, 1, 20, 0, 35),
            //    new DateTime(2020, 8, 2, 20, 1, 20));
            
            var start = new DateTime(2020, 8, 1, 19, 59, 59);
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    start = start.AddSeconds(1);
                }
            });


            A.CallTo(() => dateTimeNowProvider.UtcNow).ReturnsLazily(() => start);



            var timedExecutionService = new TimedExecutionsService(new TelemetryClient(), dateTimeNowProvider, new List<TimedExecution>
            {
                new TimedExecution
                {
                    Name = "Test",
                    Action = (a,  b) =>
                    {
                        outputHelper.WriteLine($"Executed Test 1 on {dateTimeNowProvider.UtcNow}");
                        return Task.CompletedTask;
                    },
                    EnqueueTime = new DateTime(2020, 8, 1, 20, 0, 0),
                    NextOccurence = () => dateTimeNowProvider.UtcNow.Add(new TimeSpan(0, 0, 1, 0))
                },
                new TimedExecution
                {
                    Name = "Test1",
                    Action  = (a, b) =>
                    {
                        outputHelper.WriteLine($"Executed test 2 on {dateTimeNowProvider.UtcNow}");
                        return Task.CompletedTask;
                    },
                    EnqueueTime = new DateTime(2020,8,1,19,0,15),
                    NextOccurence = () => dateTimeNowProvider.UtcNow.Add(new TimeSpan(0, 0, 0, 30))
                },
                new TimedExecution
                {
                    Name = "Test2",
                    Action  = async (a, b) =>
                    {
                        outputHelper.WriteLine($"Starting test 3 on {dateTimeNowProvider.UtcNow}");
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        outputHelper.WriteLine($"Finished test 3 on {dateTimeNowProvider.UtcNow}");
                    },
                    EnqueueTime = new DateTime(2020,8,1,19,0,15),
                    NextOccurence = () => dateTimeNowProvider.UtcNow.Add(new TimeSpan(0, 0, 0, 30))
                }
            });

            outputHelper.WriteLine($"Commencing bot on {dateTimeNowProvider.UtcNow}");

            await timedExecutionService.Start();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await timedExecutionService.Setup(client)).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await Task.Delay(90000);

            await timedExecutionService.Stop();

        }
    }
}
