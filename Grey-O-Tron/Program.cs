using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GreyOTron
{
    public class Program
    {
        private static IContainer container;
        private static DiscordSocketClient client;
        private static DiscordBotsApi discordBotsApi;
        private static TelemetryClient log;
        public static async Task Main()
        {
            container = AutofacConfiguration.Build();
            log = container.Resolve<TelemetryClient>();
            await Setup();
            Environment.Exit(-1);
        }

        private static async Task Setup()
        {
            log.TrackTrace("Bot started.");
            var configuration = container.Resolve<IConfiguration>();
            discordBotsApi = container.Resolve<DiscordBotsApi>();
            client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, configuration["GreyOTron-Token"]);
            await client.StartAsync();

            client.Ready += Ready;
            client.MessageReceived += ClientOnMessageReceived;

            //Bot should never stop.
            await Task.Delay(-1);
        }

        private static void UpdateStatistics()
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

        private static async Task Ready()
        {
            UpdateStatistics();
            var interval = TimeSpan.FromSeconds(30);
            while (true)
            {
                await client.SetGameAsync($"help on https://greyotron.eu | v{VersionResolver.Get()}");
                if (Math.Abs(DateTime.Now.TimeOfDay.Subtract(new TimeSpan(0, 21, 0, 0)).TotalMilliseconds) <= interval.TotalMilliseconds / 2)
                {
                    UpdateStatistics();
                    foreach (var guildUser in client.Guilds.SelectMany(x => x.Users))
                    {
                        try
                        {
                            var keyRepository = container.Resolve<KeyRepository>();
                            var gw2Api = container.Resolve<Gw2Api>();
                            var verifyUser = container.Resolve<VerifyUser>();
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
                                log.TrackTrace(JsonConvert.SerializeObject(guildUser, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
                            }
                            log.TrackException(e);
                        }
                    }
                }
                await Task.Delay(interval);
            }
            //Don't have to return since bot never stops anyway.
        }

        private static async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            try
            {
                var processor = container.Resolve<CommandProcessor>();
                var command = processor.Parse(socketMessage.Content);
                command.Client = client;
                await command.Execute(socketMessage);
            }
            catch (Exception e)
            {
                log.TrackTrace($"{socketMessage.Author.Username}#{socketMessage.Author.Discriminator}");
                log.TrackTrace(socketMessage.Content);
                log.TrackException(e);
            }

        }
    }
}
