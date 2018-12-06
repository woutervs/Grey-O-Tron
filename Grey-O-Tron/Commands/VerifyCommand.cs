using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.ApiClients;
using GreyOTron.Helpers;
using GreyOTron.TableStorage;

namespace GreyOTron.Commands
{
    [Command("gw2-verify")]
    public class VerifyCommand : ICommand
    {
        private readonly KeyRepository _keyRepository;
        private readonly Gw2Api _gw2Api;
        private readonly VerifyUser _verifyUser;

        public VerifyCommand(KeyRepository keyRepository, Gw2Api gw2Api, VerifyUser verifyUser)
        {
            _keyRepository = keyRepository;
            _gw2Api = gw2Api;
            _verifyUser = verifyUser;
        }

        public async Task Execute(SocketMessage message)
        {
            if (message.Author is SocketGuildUser guildUser)
            {
                var discordClientWithKey = await _keyRepository.Get("Gw2", message.Author.Id.ToString());
                if (discordClientWithKey == null)
                {
                    await message.Author.SendMessageAsync(
                        "You haven't yet registered a key with me, use the gw2-key command to do so.");
                }
                else
                {
                    var acInfo = _gw2Api.GetInformationForUserByKey(discordClientWithKey.Key);
                    if (acInfo.TokenInfo != null && acInfo.TokenInfo.Name ==
                        $"{message.Author.Username}#{message.Author.Discriminator}")
                    {
                        await _verifyUser.Verify(acInfo, guildUser);
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
        }

        public string Arguments { get; set; }
    }
}
