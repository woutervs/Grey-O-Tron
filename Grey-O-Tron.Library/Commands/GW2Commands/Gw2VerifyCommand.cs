﻿using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Attributes;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using GreyOTron.Resources;
using Polly.CircuitBreaker;

namespace GreyOTron.Library.Commands.GW2Commands
{
    [Command("gw2-verify", CommandDescription = "Use the stored Guild Wars 2 key to verify if a user belongs to worlds set by the discord server.\nAs admin you can add the user id to verify a user on your server. Eg. got#gw2-verify {nonce}", CommandOptions = CommandOptions.DiscordServer)]
    public class Gw2VerifyCommand : ICommand
    {
        private readonly IGw2DiscordUserRepository gw2ApiKeyRepository;
        private readonly Gw2Api gw2Api;
        private readonly VerifyUserHelper verifyUser;
        private readonly RemoveUserHelper removeUser;

        public Gw2VerifyCommand(IGw2DiscordUserRepository gw2ApiKeyRepository, Gw2Api gw2Api, VerifyUserHelper verifyUser, RemoveUserHelper removeUser)
        {
            this.gw2ApiKeyRepository = gw2ApiKeyRepository;
            this.gw2Api = gw2Api;
            this.verifyUser = verifyUser;
            this.removeUser = removeUser;
        }

        public async Task Execute(IMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var guildUser = (SocketGuildUser)message.Author;
            var userId = message.Author.Id.ToString();
            string context = null;
            if (!string.IsNullOrWhiteSpace(Arguments) && guildUser.IsAdminOrOwner())
            {
                userId = Arguments.Trim();
                context = userId;
            }
            var discordClientWithKey = await gw2ApiKeyRepository.Get(ulong.Parse(userId));
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
                    acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.ApiKey);
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

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
