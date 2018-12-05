using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.ApiClients;
using GreyOTron.TableStorage;

namespace GreyOTron.Commands
{
    [Command("gw2-key")]
    public class Gw2KeyCommand : ICommand
    {
        private readonly Gw2Api _gw2Api;
        private readonly DiscordGuildGw2WorldRepository _discordGuildGw2WorldRepository;
        private readonly Gw2KeyRepository _gw2KeyRepository;

        public Gw2KeyCommand(Gw2Api gw2Api, DiscordGuildGw2WorldRepository discordGuildGw2WorldRepository, Gw2KeyRepository gw2KeyRepository)
        {
            _gw2Api = gw2Api;
            _discordGuildGw2WorldRepository = discordGuildGw2WorldRepository;
            _gw2KeyRepository = gw2KeyRepository;
        }

        public async Task Execute(SocketMessage socketMessage)
        {
            if (socketMessage.Author.Id == 291207609791283212)
            {
                await socketMessage.Author.SendMessageAsync("Go back to your own corner pleb!");
            }

            var key = Arguments;
            var acInfo = _gw2Api.GetInformationForUserByKey(key);
            if (acInfo.TokenInfo != null && acInfo.TokenInfo.Name == $"{socketMessage.Author.Username}#{socketMessage.Author.Discriminator}")
            {

                if (socketMessage.Author is SocketGuildUser guildUser)
                {
                    var worlds =
                        (await _discordGuildGw2WorldRepository.Get(guildUser.Guild.Id.ToString())).Select(x =>
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

                        await _gw2KeyRepository.Set(new DiscordClientWithKey(guildUser.Guild.Id.ToString(),
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
                    await socketMessage.Author.SendMessageAsync("You must use the gw2-key command from within the discord server you try to get verified on.");
                    //await socketMessage.Author.SendMessageAsync("I've stored your key, you can now self verify on a discord server by using got#verify.");
                }
            }
            else
            {
                await socketMessage.Author.SendMessageAsync($"Please make sure your GW2 application key's name is the same as your discord username: {socketMessage.Author.Username}#{socketMessage.Author.Discriminator}");
                await socketMessage.Author.SendMessageAsync("You can view, create and edit your GW2 application key's on https://account.arena.net/applications");
            }

            await socketMessage.Channel.DeleteMessagesAsync(new List<SocketMessage> { socketMessage });
        }

        public string Arguments { get; set; }
    }
}
