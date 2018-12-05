using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Discord;
using Discord.WebSocket;
using GreyOTron.ApiClients;
using GreyOTron.Helpers;
using GreyOTron.TableStorage;
using Microsoft.Extensions.Configuration;

namespace GreyOTron
{
    public class Program
    {
        private static DiscordSocketClient client;
        private static Gw2KeyRepository gw2KeyRepository;
        private static DiscordGuildGw2WorldRepository discordGuildGw2WorldRepository;
        private static Gw2Api gw2Api;
        public static async Task Main()
        {
            var container = AutofacConfiguration.Build();

            gw2KeyRepository = container.Resolve<Gw2KeyRepository>();
            discordGuildGw2WorldRepository = container.Resolve<DiscordGuildGw2WorldRepository>();
            gw2Api = container.Resolve<Gw2Api>();

            var configuration = container.Resolve<IConfigurationRoot>();

            client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, configuration["GreyOTron-Token"]);
            await client.StartAsync();
            client.MessageReceived += ClientOnMessageReceived;
            await client.SetGameAsync("got#help");


            Console.ReadLine();
        }
        private static async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Content.StartsWith("got#gw2-key"))
            {
                if (socketMessage.Author.Id == 291207609791283212)
                {
                    await socketMessage.Author.SendMessageAsync("Go back to your own corner pleb!");
                }

                var key = socketMessage.Content.Replace("got#gw2-key", "").Trim();
                var acInfo = gw2Api.GetInformationForUserByKey(key);
                if (acInfo.TokenInfo != null && acInfo.TokenInfo.Name == $"{socketMessage.Author.Username}#{socketMessage.Author.Discriminator}")
                {

                    if (socketMessage.Author is SocketGuildUser guildUser)
                    {
                        var worlds =
                            (await discordGuildGw2WorldRepository.Get(guildUser.Guild.Id.ToString())).Select(x =>
                                x.RowKey);
                        if (acInfo.WorldInfo != null && worlds.Contains(acInfo.WorldInfo.Name.ToLowerInvariant()))
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
                                key, guildUser.Guild.Name));
                        }
                        else
                        {
                            await guildUser.SendMessageAsync(
                                "Your gw2 key does not belong to the verified worlds of this discord server, I can't assign your world role sorry!");
                        }
                    }
                    else
                    {
                        await socketMessage.Author.SendMessageAsync("I've stored your key, you can now self verify on a discord server by using got#verify.");
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
                //await socketMessage.Channel.SendMessageAsync(await DadJokes.GetJoke());
                //await socketMessage.Channel.DeleteMessagesAsync(new List<SocketMessage> { socketMessage });
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
