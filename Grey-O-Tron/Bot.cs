using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using UserExtensions = GreyOTron.Library.Helpers.UserExtensions;

namespace GreyOTron
{
    public class Bot
    {
        private readonly DiscordSocketClient client = new DiscordSocketClient();
        private readonly DiscordBotsApi discordBotsApi;
        private readonly KeyRepository keyRepository;
        private readonly Gw2Api gw2Api;
        private readonly VerifyUser verifyUser;
        private readonly CommandProcessor processor;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient log;
        private CancellationToken cancellationToken;

        public Bot(DiscordBotsApi discordBotsApi, KeyRepository keyRepository, Gw2Api gw2Api, VerifyUser verifyUser, CommandProcessor processor, IConfiguration configuration, TelemetryClient log)
        {
            this.discordBotsApi = discordBotsApi;
            this.keyRepository = keyRepository;
            this.gw2Api = gw2Api;
            this.verifyUser = verifyUser;
            this.processor = processor;
            this.configuration = configuration;
            this.log = log;
            if (ulong.TryParse(configuration["OwnerId"], out var ownerId))
            {
                UserExtensions.OwnerId = ownerId;
            }
        }

        public async Task Start(CancellationToken token)
        {
            cancellationToken = token;
            try
            {
                client.Ready += Ready;
                client.MessageReceived += ClientOnMessageReceived;

                await client.LoginAsync(TokenType.Bot, configuration["GreyOTron-Token"]);
                await client.StartAsync();

                log.TrackTrace("Bot started.");

                await Task.Delay(-1, token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                log.TrackException(ex);
            }

        }

        public async Task Stop()
        {
            client.Ready -= Ready;
            client.MessageReceived -= ClientOnMessageReceived;
            await client.LogoutAsync();
            await client.StopAsync();
            log.TrackTrace("Bot stopped.");
        }

        private void UpdateStatistics()
        {
            try
            {
                if (client.CurrentUser != null)
                {
                    discordBotsApi.UpdateStatistics(client.CurrentUser.Id.ToString(), new Statistics { ServerCount = client.Guilds.Count });
                }
            }
            catch (Exception e)
            {
                log.TrackException(e);
            }
        }

        private async Task Ready()
        {
            var messages = new Carrousel(
            new List<string> {
                $"v{VersionResolver.Get()}",
                "greyotron.eu",
                "got#help"
            });

            try
            {
                UpdateStatistics();
                var interval = TimeSpan.FromSeconds(30);

                while (true)
                {
                    await client.SetGameAsync(messages.Next());
                    if (Math.Abs(DateTime.UtcNow.TimeOfDay.Subtract(new TimeSpan(0, 20, 0, 0)).TotalMilliseconds) <= interval.TotalMilliseconds / 2)
                    {
                        UpdateStatistics();
                        var guildUsersQueue = new Queue<SocketGuildUser>(client.Guilds.SelectMany(x => x.Users));
                        log.TrackEvent("UserVerification.Started", metrics:new Dictionary<string, double> {{"Count", guildUsersQueue.Count}});
                        var stopWatch = Stopwatch.StartNew();
                        while (guildUsersQueue.TryPeek(out var guildUser))
                        {
                            await Policy.Handle<BrokenCircuitException>()
                                .WaitAndRetry(6, i => TimeSpan.FromMinutes(Math.Pow(2, i % 6)))
                                .Execute(async
                                    () =>
                                {
                                    try
                                    {
                                        var discordClientWithKey = await keyRepository.Get("Gw2", guildUser.Id.ToString());
                                        if (discordClientWithKey == null) return;
                                        AccountInfo acInfo;
                                        try
                                        {
                                            acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.Key);
                                        }
                                        catch (InvalidKeyException)
                                        {
                                            await guildUser.SendMessageAsync("Your api-key is invalid, please set a new one and re-verify.");
                                            throw;
                                        }
                                        await verifyUser.Verify(acInfo, guildUser, guildUser, true);
                                    }
                                    catch (BrokenCircuitException)
                                    {
                                        await client.SendMessageToBotOwner("Gw2 Api not recovering fast enough from repeated messages, pausing execution.");
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
                        log.TrackEvent("UserVerification.Ended", metrics: new Dictionary<string, double> { { "run-time", stopWatch.ElapsedMilliseconds } });
                    }
                    await Task.Delay(interval, cancellationToken);
                }
            }
            catch (Exception e)
            {
                log.TrackException(e);
            }
            //Don't have to return since bot never stops anyway.
            // ReSharper disable once FunctionNeverReturns
        }

        private async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            try
            {
                var command = processor.Parse(socketMessage.Content);
                command.Client = client;
                await command.Execute(socketMessage, cancellationToken);
            }
            catch (Exception e)
            {
                ExceptionHandler.HandleException(log, e, socketMessage.Author, socketMessage.Content);
            }
        }
    }
}
