using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.RepositoryInterfaces;
using GreyOTron.Resources;
using Polly.CircuitBreaker;

namespace GreyOTron.Library.Helpers
{
    public class UserJoined
    {
        private readonly IGw2DiscordServerRepository gw2DiscordServerRepository;
        private readonly IGw2DiscordUserRepository gw2DiscordUserRepository;
        private readonly Gw2Api gw2Api;
        private readonly RemoveUser removeUser;
        private readonly VerifyUser verifyUser;

        public UserJoined(IGw2DiscordServerRepository gw2DiscordServerRepository, IGw2DiscordUserRepository gw2DiscordUserRepository, Gw2Api gw2Api, RemoveUser removeUser, VerifyUser verifyUser)
        {
            this.gw2DiscordServerRepository = gw2DiscordServerRepository;
            this.gw2DiscordUserRepository = gw2DiscordUserRepository;
            this.gw2Api = gw2Api;
            this.removeUser = removeUser;
            this.verifyUser = verifyUser;
        }

        public async Task Execute(DiscordSocketClient client, SocketGuildUser joinedUser, CancellationToken cancellationToken)
        {
            var guild = await gw2DiscordServerRepository.Get(joinedUser.Guild.Id);
            if (guild.MainWorld != null || guild.Worlds.Any())
            {
                var user = await gw2DiscordUserRepository.Get(joinedUser.Id);
                if (user != null)
                {
                    await joinedUser.InternalSendMessageAsync(nameof(GreyOTronResources.AlreadyHasGw2KeyRegisteredAutoVerifyOnJoin), joinedUser.Username, joinedUser.Guild.Name);
                    AccountInfo acInfo;
                    try
                    {
                        acInfo = gw2Api.GetInformationForUserByKey(user.ApiKey);
                    }
                    catch (BrokenCircuitException)
                    {
                        await joinedUser.InternalSendMessageAsync(nameof(GreyOTronResources.GW2ApiUnableToProcess));
                        throw;
                    }
                    catch (InvalidKeyException)
                    {
                        await joinedUser.InternalSendMessageAsync(nameof(GreyOTronResources.InvalidKey), nameof(GreyOTronResources.Your));
                        await removeUser.Execute(client, joinedUser, client.Guilds, cancellationToken);
                        throw;
                    }
                    await verifyUser.Execute(acInfo, joinedUser, joinedUser);
                }
                else
                {
                    await joinedUser.InternalSendMessageAsync(
                        nameof(GreyOTronResources.NoGw2KeyRegisteredAutoVerifyOnJoin), joinedUser.Username, joinedUser.Guild.Name);
                }
                
                
            }
        }
    }
}
