using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-verify", CommandDescription = "Use the stored Guild Wars 2 key to verify if a user belongs to worlds set by the discord server.", CommandOptions = CommandOptions.DiscordServer)]
    public class VerifyCommand : ICommand
    {
        private readonly KeyRepository keyRepository;
        private readonly Gw2Api gw2Api;
        private readonly VerifyUser verifyUser;

        public VerifyCommand(KeyRepository keyRepository, Gw2Api gw2Api, VerifyUser verifyUser)
        {
            this.keyRepository = keyRepository;
            this.gw2Api = gw2Api;
            this.verifyUser = verifyUser;
        }

        public async Task Execute(SocketMessage message)
        {
            if (message.Author is SocketGuildUser guildUser)
            {
                var discordClientWithKey = await keyRepository.Get("Gw2", message.Author.Id.ToString());
                if (discordClientWithKey == null)
                {
                    await message.Author.SendMessageAsync(
                        "You haven't yet registered a key with me, use the gw2-key command to do so.");
                }
                else
                {
                    var acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.Key);
                    if (acInfo == null)
                    {
                        await message.Author.SendMessageAsync("The GW2Api is unavailable at this time, please try again later.");
                    }
                    else if (!string.IsNullOrWhiteSpace(discordClientWithKey.Key) && acInfo.TokenInfo != null && acInfo.TokenInfo.Name ==
                      $"{message.Author.Username}#{message.Author.Discriminator}")
                    {
                        await verifyUser.Verify(acInfo, guildUser);
                    }
                    else
                    {
                        await message.Author.SendMessageAsync("It seems like it your stored key has become invalid, please renew it using the gw2-key command.");
                    }
                }
            }
            else
            {
                await message.Author.SendMessageAsync("You must use the gw2-verify command from within the discord server you try to get verified on.");
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
