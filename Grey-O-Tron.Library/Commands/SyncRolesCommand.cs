using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;
using GreyOTron.Library.Translations;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GreyOTron.Library.Commands
{
    [Command("sync-roles", CommandDescription = "Cleans up duplicate roles on a server", CommandArguments = "", CommandOptions = CommandOptions.DiscordServer | CommandOptions.RequiresAdmin)]
    public class SyncRolesCommand : ICommand
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;
        private readonly Cache cache;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient log;

        public SyncRolesCommand(DiscordGuildSettingsRepository discordGuildSettingsRepository, Cache cache, IConfiguration configuration, TelemetryClient log)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
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
                    var worlds = JsonConvert.DeserializeObject<List<string>>((await discordGuildSettingsRepository.Get(DiscordGuildSetting.Worlds, guildUser.Guild.Id.ToString()))?.Value ?? "[]");
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
                    await guildUser.InternalSendMessageAsync("Unauthorized to cleanup roles on this server.");
                }
            }
            else
            {
                await message.Author.InternalSendMessageAsync("You have to use this command from within a server.");
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
