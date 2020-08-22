using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Interfaces;
using GreyOTron.Resources;
using Microsoft.ApplicationInsights;
using Polly;
using Polly.CircuitBreaker;

namespace GreyOTron.Library.Helpers
{
    public class VerifyAllHelper
    {
        private readonly TelemetryClient log;
        private readonly IGw2DiscordUserRepository gw2ApiKeyRepository;
        private readonly Gw2Api gw2Api;
        private readonly RemoveUserHelper removeUser;
        private readonly VerifyUserHelper verifyUser;

        public VerifyAllHelper(TelemetryClient log, IGw2DiscordUserRepository gw2ApiKeyRepository, Gw2Api gw2Api, RemoveUserHelper removeUser, VerifyUserHelper verifyUser)
        {
            this.log = log;
            this.gw2ApiKeyRepository = gw2ApiKeyRepository;
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
                                await gw2ApiKeyRepository.Get(guildUser.Id);
                            if (discordClientWithKey == null) return;
                            AccountInfo acInfo;
                            try
                            {
                                acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.ApiKey);
                            }
                            catch (InvalidKeyException)
                            {
                                await guildUser.InternalSendMessageAsync(nameof(GreyOTronResources.InvalidApiKey));
                                await removeUser.Execute(client, guildUser, client.Guilds, cancellationToken);
                                throw;
                            }

                            await verifyUser.Execute(acInfo, guildUser, guildUser, true);
                        }
                        catch (Exception e)
                        {
                            ExceptionHandler.HandleException(client, log, e, guildUser);
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
