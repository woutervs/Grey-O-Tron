using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;
using GreyOTron.Library.Translations;
using Polly.CircuitBreaker;
using System.Threading;
using System.Threading.Tasks;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-verify", CommandDescription = "Use the stored Guild Wars 2 key to verify if a user belongs to worlds set by the discord server.", CommandOptions = CommandOptions.DiscordServer)]
    public class VerifyCommand : ICommand
    {
        private readonly KeyRepository keyRepository;
        private readonly Gw2Api gw2Api;
        private readonly VerifyUser verifyUser;
        private readonly RemoveUser removeUser;

        public VerifyCommand(KeyRepository keyRepository, Gw2Api gw2Api, VerifyUser verifyUser, RemoveUser removeUser)
        {
            this.keyRepository = keyRepository;
            this.gw2Api = gw2Api;
            this.verifyUser = verifyUser;
            this.removeUser = removeUser;
        }

        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (message.Author is SocketGuildUser guildUser)
            {
                var userId = message.Author.Id.ToString();
                string context = null;
                if (!string.IsNullOrWhiteSpace(Arguments) && guildUser.IsAdminOrOwner())
                {
                    userId = Arguments.Trim();
                    context = userId;
                }
                var discordClientWithKey = await keyRepository.Get("Gw2", userId);
                if (discordClientWithKey == null)
                {
                    if (context != null)
                    {
                        await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.KeyNotFound), context);
                    }
                    else
                    {
                        await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.KeyNotRegistered));
                    }
                }
                else
                {
                    var userToUpdate = guildUser;
                    var contextUser = guildUser;
                    if (context != null)
                    {
                        var couldParse = ulong.TryParse(context, out var contextId);
                        userToUpdate = userToUpdate.Guild.GetUser(contextId);
                        if (userToUpdate == null || !couldParse)
                        {
                            await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.UserNotFoundForVerification));
                            if (!(message.Channel is SocketDMChannel))
                            {
                                await message.DeleteAsync();
                            }
                            return;
                        }
                    }
                    AccountInfo acInfo;
                    try
                    {
                        acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.Key);
                    }
                    catch (BrokenCircuitException)
                    {
                        await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.GW2ApiUnableToProcess));
                        throw;
                    }
                    catch (InvalidKeyException)
                    {
                        await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.InvalidKey), guildUser.Id != contextUser.Id ? guildUser.Username + "'s" : "Your");
                        await removeUser.Execute(Client, guildUser, Client.Guilds, cancellationToken);
                        throw;
                    }
                    await verifyUser.Execute(acInfo, userToUpdate, contextUser);
                }
            }
            else
            {
                await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.TriedVerifyWhilstNotOnServer));
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
