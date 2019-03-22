using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-key", CommandDescription = "Stores Guild Wars 2 key in the database.", CommandArguments = "{key}", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class Gw2KeyCommand : ICommand
    {
        private readonly Gw2Api gw2Api;
        private readonly KeyRepository gw2KeyRepository;
        private readonly IConfiguration configuration;
        private readonly VerifyUser verifyUser;

        public Gw2KeyCommand(Gw2Api gw2Api, KeyRepository gw2KeyRepository, IConfiguration configuration, VerifyUser verifyUser)
        {
            this.gw2Api = gw2Api;
            this.gw2KeyRepository = gw2KeyRepository;
            this.configuration = configuration;
            this.verifyUser = verifyUser;
        }

        public async Task Execute(SocketMessage message)
        {
            var key = Arguments;
            if (string.IsNullOrWhiteSpace(key))
            {
                await message.Author.SendMessageAsync("This key seems empty to me, please try again.");
            }
            else
            {
                var acInfo = gw2Api.GetInformationForUserByKey(key);
                if (acInfo?.TokenInfo?.Name == $"{message.Author.Username}#{message.Author.Discriminator}")
                {
                    await gw2KeyRepository.Set(new DiscordClientWithKey("Gw2", message.Author.Id.ToString(),
                        $"{message.Author.Username}#{message.Author.Discriminator}", key));

                    if (message.Author is SocketGuildUser guildUser)
                    {
                        await verifyUser.Verify(acInfo, guildUser);
                    }
                    else
                    {
                        await message.Author.SendMessageAsync($"Your key has been stored, don't forget to use {configuration["CommandPrefix"]}gw2-verify on the server you wish to get verified on.");
                    }
                }
                else
                {
                    await message.Author.SendMessageAsync($"Please make sure your GW2 application key's name is the same as your discord username: {message.Author.Username}#{message.Author.Discriminator}");
                    await message.Author.SendMessageAsync("You can view, create and edit your GW2 application key's on https://account.arena.net/applications");
                }
            }

            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
