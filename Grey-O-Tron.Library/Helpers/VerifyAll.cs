using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.TableStorage;
using Microsoft.ApplicationInsights;
using Polly;
using Polly.CircuitBreaker;

namespace GreyOTron.Library.Helpers
{
    public class VerifyAll
    {
        private readonly TelemetryClient log;
        private readonly KeyRepository keyRepository;
        private readonly Gw2Api gw2Api;
        private readonly RemoveUser removeUser;
        private readonly VerifyUser verifyUser;

        public VerifyAll(TelemetryClient log, KeyRepository keyRepository, Gw2Api gw2Api, RemoveUser removeUser, VerifyUser verifyUser)
        {
            this.log = log;
            this.keyRepository = keyRepository;
            this.gw2Api = gw2Api;
            this.removeUser = removeUser;
            this.verifyUser = verifyUser;
        }

        public async Task Execute(DiscordSocketClient client, CancellationToken cancellationToken)
        {
            await client.SetGameAsync("Verifying users.");
            var guildUsersQueue = new Queue<SocketGuildUser>(client.Guilds.SelectMany(x => x.Users));
            log.TrackEvent("UserVerification.Started",
                metrics: new Dictionary<string, double> { { "Count", guildUsersQueue.Count } });
            var stopWatch = Stopwatch.StartNew();
            while (guildUsersQueue.TryPeek(out var guildUser) && !cancellationToken.IsCancellationRequested)
            {
                await Policy.Handle<BrokenCircuitException>()
                    .WaitAndRetry(6, i => TimeSpan.FromMinutes(Math.Pow(2, i % 6)))
                    .Execute(async
                        () =>
                    {
                        try
                        {
                            var discordClientWithKey =
                                await keyRepository.Get("Gw2", guildUser.Id.ToString());
                            if (discordClientWithKey == null) return;
                            AccountInfo acInfo;
                            try
                            {
                                acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.Key);
                            }
                            catch (InvalidKeyException)
                            {
                                await guildUser.InternalSendMessageAsync(
                                    "Your api-key is invalid, please set a new one and re-verify.");
                                await removeUser.Execute(guildUser, client.Guilds, cancellationToken);
                                throw;
                            }

                            await verifyUser.Execute(acInfo, guildUser, guildUser, true);
                        }
                        catch (BrokenCircuitException)
                        {
                            await client.SendMessageToBotOwner(
                                "Gw2 Api not recovering fast enough from repeated messages, pausing execution.");
                            throw;
                        }
                        catch (Exception e)
                        {
                            ExceptionHandler.HandleException(log, e, guildUser);
                        }
                    });
                guildUsersQueue.Dequeue();
            }
            stopWatch.Stop();
            log.TrackEvent("UserVerification.Ended",
                new Dictionary<string, string> { { "run-time", stopWatch.Elapsed.ToString("c") } });
        }
    }
}
