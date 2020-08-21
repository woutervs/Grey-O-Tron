using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Models;
using GreyOTron.Library.RepositoryInterfaces;
using GreyOTron.Resources;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Library.Commands
{
    [Command("sync-roles", CommandDescription = "Cleans up duplicate roles on a server", CommandArguments = "", CommandOptions = CommandOptions.DiscordServer | CommandOptions.RequiresAdmin)]
    public class SyncRolesCommand : ICommand
    {
        private readonly IGw2DiscordServerRepository gw2DiscordServerRepository;
        private readonly Cache cache;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient log;

        public SyncRolesCommand(IGw2DiscordServerRepository gw2DiscordServerRepository, Cache cache, IConfiguration configuration, TelemetryClient log)
        {
            this.gw2DiscordServerRepository = gw2DiscordServerRepository;
            this.cache = cache;
            this.configuration = configuration;
            this.log = log;
        }


        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (message.Author is SocketGuildUser guildUser)
            {
                if (guildUser.IsAdminOrOwner())
                {
                    var worlds = ((await gw2DiscordServerRepository.Get(guildUser.Guild.Id))?.Worlds ?? new List<Gw2WorldDto>()).Select(x=>x.Name).ToList();
                    worlds.Add(configuration["LinkedServerRole"]);
                    foreach (var world in worlds)
                    {
                        var roles = guildUser.Guild.Roles.Where(x => x.Name == world);
                        var socketRoles = roles.OrderBy(x => x.CreatedAt).ToList();
                        using (var enumerator = socketRoles.GetEnumerator())
                        {
                            if (enumerator.MoveNext())
                            {
                                var firstRole = enumerator.Current;
                                if (firstRole != null)
                                {
                                    cache.Replace($"roles::{guildUser.Guild.Id}::{firstRole.Name}", firstRole, TimeSpan.FromDays(1));
                                    while (enumerator.MoveNext())
                                    {
                                        var role = enumerator.Current;
                                        if (role != null)
                                        {
                                            foreach (var member in role.Members.ToList())
                                            {
                                                try
                                                {
                                                    await member.AddRoleAsync(firstRole);
                                                    await member.RemoveRoleAsync(role);
                                                }
                                                catch (Exception e)
                                                {
                                                    ExceptionHandler.HandleException(Client, log, e, member,
                                                        "sync-roles for user error");
                                                }

                                            }

                                            try
                                            {
                                                await role.DeleteAsync();
                                            }
                                            catch (Exception e)
                                            {
                                                ExceptionHandler.HandleException(Client, log, e, guildUser,
                                                    $"sync-roles for deletion error role name:{role.Name} role id: {role.Id}");
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                    await guildUser.InternalSendMessageAsync("Guild roles are synced.");
                }
                else
                {
                    await guildUser.InternalSendMessageAsync(nameof(GreyOTronResources.AdministrativePermissionsOnly), "sync-roles");
                }
            }
            else
            {
                await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.ServerOnlyCommand), "sync-roles");
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
