using System.Threading;
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

        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (message.Author is SocketGuildUser guildUser)
            {
                var userId = message.Author.Id.ToString();
                string context = null;
                if (!string.IsNullOrWhiteSpace(Arguments) && guildUser.GuildPermissions.Administrator)
                {
                    userId = Arguments.Trim();
                    context = userId;
                }
                var discordClientWithKey = await keyRepository.Get("Gw2", userId);
                if (discordClientWithKey == null)
                {
                    if (context != null)
                    {
                        await message.Author.SendMessageAsync($"No key found for {context}");
                    }
                    else
                    {
                        await message.Author.SendMessageAsync(
                            "You haven't yet registered a key with me, use the gw2-key command to do so.");
                    }
                }
                else
                {
                    if (context != null)
                    {
                        context = discordClientWithKey.Username;
                    }
                    var acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.Key);
                    await verifyUser.Verify(acInfo, guildUser, false, context);
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
