using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace GreyOTron
{
    public class Program
    {
        private static DiscordSocketClient client;
        private static Gw2KeyRepository gw2KeyRepository;
        private static DiscordGuildGw2WorldRepository discordGuildGw2WorldRepository;
        public static async Task Main()
        {
            BootstrapConfiguration();
            client = new DiscordSocketClient();
            var token = Configuration["GreyOTron-Token"];
            gw2KeyRepository = new Gw2KeyRepository(Configuration["StorageConnectionString"]);
            discordGuildGw2WorldRepository = new DiscordGuildGw2WorldRepository(Configuration["StorageConnectionString"]);
            AppDomain.CurrentDomain.UnhandledException += async (sender, args) =>
            {
                try
                {
                    client.Dispose();
                    client = new DiscordSocketClient();
                    await StartClient(token);
                }
                catch
                {
                    // ignored
                }
            };

            await StartClient(token);
            Console.ReadLine();
        }

        private static async Task StartClient(string token)
        {
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            client.MessageReceived += ClientOnMessageReceived;
            await client.SetGameAsync("got#help");
        }

        private static void BootstrapConfiguration()
        {
            Trace.WriteLine("Setting up configuration");
            var builder = new ConfigurationBuilder();
            string env = Environment.GetEnvironmentVariable("Environment");
            Trace.WriteLine(env);

            if (env == "Development")
            {
                builder.AddUserSecrets<Program>();
            }
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public static IConfigurationRoot Configuration { get; set; }

        private static async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Content.StartsWith("got#gw2-key"))
            {
                var key = socketMessage.Content.Replace("got#gw2-key", "").Trim();
                var acInfo = Gw2Api.GetInformationForUserByKey(key);
                if (acInfo.TokenInfo.Name == $"{socketMessage.Author.Username}#{socketMessage.Author.Discriminator}")
                {

                    if (socketMessage.Author is SocketGuildUser guildUser)
                    {
                        var worlds =
                            (await discordGuildGw2WorldRepository.Get(guildUser.Guild.Id.ToString())).Select(x =>
                                x.RowKey);
                        if (worlds.Contains(acInfo.WorldInfo.Name.ToLowerInvariant()))
                        {
                            var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Name == acInfo.WorldInfo.Name);


                            if (role == null)
                            {
                                var restRole =
                                    await guildUser.Guild.CreateRoleAsync(acInfo.WorldInfo.Name, GuildPermissions.None);
                                await guildUser.AddRoleAsync(restRole);
                            }
                            else
                            {
                                await guildUser.AddRoleAsync(role);
                            }

                            await gw2KeyRepository.Set(new DiscordClientWithKey(guildUser.Guild.Id.ToString(),
                                guildUser.Id.ToString(),
                                $"{socketMessage.Author.Username}#{socketMessage.Author.Discriminator}",
                                acInfo.TokenInfo.Id, guildUser.Guild.Name));
                        }
                    }
                    else
                    {
                        await socketMessage.Author.SendMessageAsync(
                            "You will have to use the got#gw2-key command from the server you want to be assigned to.");
                    }
                }
                else
                {
                    await socketMessage.Author.SendMessageAsync($"Please make sure your GW2 application key's name is the same as your discord username: {socketMessage.Author.Username}#{socketMessage.Author.Discriminator}");
                    await socketMessage.Author.SendMessageAsync("You can view, create and edit your GW2 application key's on https://account.arena.net/applications");
                }

                await socketMessage.Channel.DeleteMessagesAsync(new List<SocketMessage> { socketMessage });
            }

            if (socketMessage.Content.StartsWith("got#joke"))
            {
                await socketMessage.Channel.SendMessageAsync(await DadJokes.GetJoke());
                await socketMessage.Channel.DeleteMessagesAsync(new List<SocketMessage> { socketMessage });
            }

            if (socketMessage.Content.StartsWith("got#help"))
            {
                await socketMessage.Author.SendMessageAsync($"Currently I know the following commands:" +
                                                            $"\n\n**got#joke**" +
                                                            $"\n\n**got#gw2-key key**\nYour GW2 application key can be set on https://account.arena.net/applications as name you have to use your discord username: **{socketMessage.Author.Username}#{socketMessage.Author.Discriminator}**" +
                                                            $"\n\n**got#set-worlds worldname;otherworldname**\nYou must have administrative permissions to perform this command.");
                await socketMessage.Channel.DeleteMessagesAsync(new List<SocketMessage> { socketMessage });
            }

            if (socketMessage.Content.StartsWith("got#set-worlds"))
            {
                var worlds = socketMessage.Content.Replace("got#set-worlds", "").Trim().TrimEnd(';', ',').Split(';', ',');
                if (socketMessage.Author is SocketGuildUser guildUser)
                {
                    if (guildUser.GuildPermissions.Administrator)
                    {
                        await discordGuildGw2WorldRepository.Clear(guildUser.Guild.Id.ToString());
                        foreach (var world in worlds)
                        {
                            await discordGuildGw2WorldRepository.Set(new DiscordGw2World(guildUser.Guild.Id.ToString(),
                                world.ToLowerInvariant()));
                            await guildUser.SendMessageAsync($"{world} set for {guildUser.Guild.Name}");
                        }
                    }
                    else
                    {
                        await guildUser.SendMessageAsync(
                            "You must have administrative permissions to perform the got#set-worlds command.");
                    }
                }
                await socketMessage.Channel.DeleteMessagesAsync(new List<SocketMessage> { socketMessage });
            }
            await Task.CompletedTask;
        }
    }
}
