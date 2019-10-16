using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;
using Polly.CircuitBreaker;

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
                        await message.Author.InternalSendMessageAsync($"No key found for {context}");
                    }
                    else
                    {
                        await message.Author.InternalSendMessageAsync(
                            "You haven't yet registered a key with me, use the gw2-key command to do so.");
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
                            await contextUser.InternalSendMessageAsync($"User you are trying to verify was not found on this Discord server.");
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
                        await message.Author.InternalSendMessageAsync("The GW2 api can't handle this request at the time, please try again a bit later.");
                        throw;
                    }
                    catch (InvalidKeyException)
                    {
                        await contextUser.InternalSendMessageAsync($"{(guildUser.Id != contextUser.Id ? guildUser.Username + "'s" : "Your")} api-key is invalid, please set a new one and re-verify.");
                        await removeUser.Execute(guildUser, Client.Guilds, cancellationToken);
                        throw;
                    }
                    await verifyUser.Execute(acInfo, userToUpdate, contextUser);
                }
            }
            else
            {
                await message.Author.InternalSendMessageAsync("You must use the gw2-verify command from within the discord server you try to get verified on.");
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
