using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Models;
using GreyOTron.Resources;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Library.Helpers
{
    public class RemoveUserHelper
    {
        private readonly IGw2DiscordUserRepository gw2Gw2ApiKeyRepository;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient log;
        private readonly IGw2DiscordServerRepository gw2DiscordServerRepository;

        public RemoveUserHelper(IGw2DiscordUserRepository gw2Gw2ApiKeyRepository, IConfiguration configuration, TelemetryClient log, IGw2DiscordServerRepository gw2DiscordServerRepository)
        {
            this.gw2Gw2ApiKeyRepository = gw2Gw2ApiKeyRepository;
            this.configuration = configuration;
            this.log = log;
            this.gw2DiscordServerRepository = gw2DiscordServerRepository;
        }


        public async Task Execute(DiscordSocketClient client, IUser user, IEnumerable<SocketGuild> guilds, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            var key = (await gw2Gw2ApiKeyRepository.Get(user.Id)).ApiKey;
            log.TrackTrace($"gw2-key-removal {key}", new Dictionary<string, string> { { "DiscordUser", user.UserId() }, { "DiscordUserId", user.Id.ToString() } });
            await gw2Gw2ApiKeyRepository.RemoveApiKey(user.Id);
            var affectedServers = new List<string>();
            foreach (var guild in guilds)
            {
                try
                {
                    var gw2DiscordServer = await gw2DiscordServerRepository.Get(guild.Id);

                    var worlds = (gw2DiscordServer?.Worlds ?? new List<Gw2WorldDto>()).Select(x => x.Name).ToList();

                    if (gw2DiscordServer?.MainWorld != null && !worlds.Any(y => y.Equals(gw2DiscordServer.MainWorld.Name)))
                    {
                        worlds.Add(gw2DiscordServer.MainWorld.Name);
                    }

                    var guildUser = guild.Users.FirstOrDefault(u => u.Id == user.Id);
                    if (guildUser != null)
                    {
                        affectedServers.Add(guild.Name);
                        var userOwnedRolesMatchingWorlds = guildUser.Roles.Where(x =>
                            worlds.Any(y => y.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase)) || x.Name.Equals(
                                configuration["LinkedServerRole"],
                                StringComparison.InvariantCultureIgnoreCase)).ToList();
                        await guildUser.RemoveRolesAsync(userOwnedRolesMatchingWorlds,
                            new RequestOptions { AuditLogReason = "User removed Gw2Api Key" });
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHandler.HandleException(client, log, ex, user, $"Try delete roles in {guild.Name}.");
                }
            }
            await user.InternalSendMessageAsync(nameof(GreyOTronResources.RolesWereRemoved), affectedServers.Any() ? affectedServers.Aggregate("", (a, b) => $"{a}, {b} ").TrimEnd(',', ' ') : nameof(GreyOTronResources.RolesWereRemovedNoWhere));
        }
    }
}
