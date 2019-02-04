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
        private static readonly TelemetryClient Log = new TelemetryClient();
        public static async Task Main()
        {
            container = AutofacConfiguration.Build();
            await Setup();
            Environment.Exit(-1);
        }

        private static async Task Setup()
        {
            Log.TrackTrace("Bot started.");
            var configuration = container.Resolve<IConfigurationRoot>();
            client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, configuration["GreyOTron-Token"]);
            await client.StartAsync();
            await client.SetGameAsync($"{configuration["command-prefix"]}help | greyotron.eu | v{VersionResolver.Get()}");

            var isLoggedIn = false;
            client.LoggedIn += async () =>
            {
                isLoggedIn = true;
                await Task.CompletedTask;
            };

            client.MessageReceived += ClientOnMessageReceived;
            var interval = TimeSpan.FromSeconds(10);
            while (isLoggedIn == false || client.ConnectionState == ConnectionState.Connecting || client.ConnectionState == ConnectionState.Connected)
            {               
                await client.SetGameAsync($"{configuration["command-prefix"]}help | greyotron.eu | v{VersionResolver.Get()}");
                SocketGuildUser currentUser = null;
                try
                {
                    if (Math.Abs(DateTime.Now.TimeOfDay.Subtract(new TimeSpan(0, 23, 0, 0)).TotalMilliseconds) <= interval.TotalMilliseconds / 2)
                    {
                        foreach (var guildUser in client.Guilds.SelectMany(x => x.Users))
                        {
                            currentUser = guildUser;
                            var keyRepository = container.Resolve<KeyRepository>();
                            var gw2Api = container.Resolve<Gw2Api>();
                            var verifyUser = container.Resolve<VerifyUser>();
                            var discordClientWithKey = await keyRepository.Get("Gw2", guildUser.Id.ToString());
                            if (discordClientWithKey == null) continue;
                            var acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.Key);
                            await verifyUser.Verify(acInfo, guildUser, true);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (currentUser != null)
                    {
                        Log.TrackTrace(JsonConvert.SerializeObject(currentUser, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
                    }
                    Log.TrackException(e);
                }

                
                await Task.Delay(interval);
            }
        }

        private static async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            try
            {
                var processor = container.Resolve<CommandProcessor>();
                var command = processor.Parse(socketMessage.Content);
                await command.Execute(socketMessage);
            }
            catch (Exception e)
            {
                Log.TrackTrace($"{socketMessage.Author.Username}#{socketMessage.Author.Discriminator}");
                Log.TrackTrace(socketMessage.Content);
                Log.TrackException(e);
            }

        }
    }
}
