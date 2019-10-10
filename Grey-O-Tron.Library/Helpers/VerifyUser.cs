using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.TableStorage;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GreyOTron.Library.Helpers
{
    public class VerifyUser
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;
        private readonly Cache cache;
        private readonly IConfiguration configuration;

        public VerifyUser(DiscordGuildSettingsRepository discordGuildSettingsRepository, Cache cache, IConfiguration configuration)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
            this.cache = cache;
            this.configuration = configuration;
        }

        public async Task Execute(AccountInfo gw2AccountInfo, SocketGuildUser guildUser, SocketGuildUser contextUser, bool bypassMessages = false)
        {
            var contextUserIsNotGuildUser = guildUser.Id != contextUser.Id;

            var worlds = JsonConvert.DeserializeObject<List<string>>((await discordGuildSettingsRepository.Get(DiscordGuildSetting.Worlds, guildUser.Guild.Id.ToString()))?.Value ?? "[]");
            var mainWorld =
                (await discordGuildSettingsRepository.Get(DiscordGuildSetting.MainWorld, guildUser.Guild.Id.ToString()))?.Value;

            if (mainWorld != null && !worlds.Contains(mainWorld))
            {
                worlds.Add(mainWorld);
            }

            var userOwnedRolesMatchingWorlds = guildUser.Roles.Where(x => worlds.Contains(x.Name.ToLowerInvariant()) || x.Name.Equals(configuration["LinkedServerRole"], StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (gw2AccountInfo.TokenInfo.Name != $"{guildUser.Username}#{guildUser.Discriminator}")
            {
                await contextUser.InternalSendMessageAsync(
                    $"{(contextUserIsNotGuildUser ? guildUser.Username : "You've")} most likely changed {(contextUserIsNotGuildUser ? "his/her" : "your")} discord username from {gw2AccountInfo.TokenInfo.Name} to {guildUser.Username}#{guildUser.Discriminator}." +
                    $"\n{(contextUserIsNotGuildUser ? $"Please ask {guildUser.Username} to update his/her key." : "Please update your key.")}" +
                    "\nYou can view, create and edit your GW2 application key's on https://account.arena.net/applications");
            }
            else
            {
                if (gw2AccountInfo.WorldInfo != null && worlds.Contains(gw2AccountInfo.WorldInfo.Name.ToLowerInvariant()))
                {
                    await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, contextUser, userOwnedRolesMatchingWorlds, gw2AccountInfo.WorldInfo.Name, bypassMessages);

                }
                else if (gw2AccountInfo.WorldInfo != null && gw2AccountInfo.WorldInfo.LinkedWorlds.Any(x => string.Equals(x.Name, mainWorld, StringComparison.InvariantCultureIgnoreCase)))
                {
                    await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, contextUser, userOwnedRolesMatchingWorlds,
                        configuration["LinkedServerRole"], bypassMessages);

                }
                else
                {
                    if (!bypassMessages)
                    {
                        if (gw2AccountInfo.WorldInfo == null)
                        {
                            await contextUser.InternalSendMessageAsync($"Could not assign world roles on '{guildUser.Guild.Name}' for {(contextUserIsNotGuildUser ? guildUser.Username : "you")}.");
                        }
                        else
                        {
                            await contextUser.InternalSendMessageAsync(
                                $"Your gw2 world does not belong to the verified worlds of '{guildUser.Guild.Name}' discord server, I can't assign {(contextUserIsNotGuildUser ? guildUser.Username : "your")} world role sorry!");
                        }
                    }
                }
            }

            foreach (var userOwnedRolesMatchingWorld in userOwnedRolesMatchingWorlds)
            {
                try
                {
                    await guildUser.RemoveRoleAsync(userOwnedRolesMatchingWorld);
                }
                catch (Exception e)
                {
                    throw new RemoveRoleException(userOwnedRolesMatchingWorld.Name, e);
                }

            }
        }

        private async Task CreateRoleIfNotExistsAndAssignIfNeeded(SocketGuildUser guildUser, SocketGuildUser contextUser, List<SocketRole> userOwnedRolesMatchingWorlds, string roleName, bool bypassMessages)
        {
            var contextUserIsNotGuildUser = guildUser.Id != contextUser.Id;
            var roleExistsAlready = userOwnedRolesMatchingWorlds.FirstOrDefault(x =>
                string.Equals(x.Name, roleName, StringComparison.InvariantCultureIgnoreCase));
            if (roleExistsAlready == null)
            {
                var cachedName = $"roles::{guildUser.Guild.Id}::{roleName}";
                var role = cache.GetFromCacheSliding(cachedName, TimeSpan.FromDays(1), () =>
                {
                    return guildUser.Guild.Roles.FirstOrDefault(x => x.Name == roleName) ?? (IRole)guildUser.Guild.CreateRoleAsync(roleName, GuildPermissions.None).Result;
                });
                try
                {
                    await guildUser.AddRoleAsync(role);
                }
                catch (Exception)
                {
                    cache.RemoveFromCache(cachedName);
                    await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, contextUser, userOwnedRolesMatchingWorlds, roleName, bypassMessages);
                }
                if (!bypassMessages)
                {
                    await contextUser.InternalSendMessageAsync($"{(contextUserIsNotGuildUser ? guildUser.Username : "You")} {(contextUserIsNotGuildUser ? "has" : "have")} been assigned role: {roleName} on {guildUser.Guild.Name}");
                }
            }
            userOwnedRolesMatchingWorlds.Remove(roleExistsAlready);
        }
    }
}
