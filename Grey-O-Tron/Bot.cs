using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;

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
            UpdateStatistics();
            var interval = TimeSpan.FromSeconds(30);
            while (true)
            {
                await client.SetGameAsync($"help on https://greyotron.eu | v{VersionResolver.Get()}");
                if (Math.Abs(DateTime.UtcNow.TimeOfDay.Subtract(new TimeSpan(0, 20, 0, 0)).TotalMilliseconds) <= interval.TotalMilliseconds / 2)
                {
                    UpdateStatistics();
                    foreach (var guildUser in client.Guilds.SelectMany(x => x.Users))
                    {
                        try
                        {
                            var discordClientWithKey = await keyRepository.Get("Gw2", guildUser.Id.ToString());
                            if (discordClientWithKey == null) continue;
                            var acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.Key);
                            if (acInfo != null)
                            {
                                await verifyUser.Verify(acInfo, guildUser, true);
                            }
                        }
                        catch (Exception e)
                        {
                            if (guildUser != null)
                            {
                                log.TrackException(e, new Dictionary<string, string> { { "DiscordUser", $"{guildUser.Username}#{guildUser.Discriminator}" } });
                            }
                            else
                            {
                                log.TrackException(e);
                            }


                        }
                    }
                }
                await Task.Delay(interval);
            }
            //Don't have to return since bot never stops anyway.
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
                log.TrackException(e, new Dictionary<string, string>
                {
                    {"DiscordUser", $"{socketMessage.Author.Username}#{socketMessage.Author.Discriminator}" },
                    {"Command", socketMessage.Content }

                });
            }

        }
    }
}
