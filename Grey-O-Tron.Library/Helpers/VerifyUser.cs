using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.TableStorage;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;

namespace GreyOTron.Library.Helpers
{
    public class VerifyUser
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;
        private readonly Cache cache;
        private const string LinkedServerRole = "Linked Server";

        public VerifyUser(DiscordGuildSettingsRepository discordGuildSettingsRepository, Cache cache)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
            this.cache = cache;
        }

        public async Task Verify(AccountInfo gw2AccountInfo, SocketGuildUser guildUser, bool bypassMessages = false, string context = null)
        {
            if (gw2AccountInfo == null)
            {
                if (!bypassMessages)
                {
                    await guildUser.SendMessageAsync("The GW2Api is unavailable at this time, please try again later.");
                }

                return;
            }

            var worlds = JsonConvert.DeserializeObject<List<string>>((await discordGuildSettingsRepository.Get(DiscordGuildSetting.Worlds, guildUser.Guild.Id.ToString()))?.Value ?? "[]");
            var mainWorld =
                (await discordGuildSettingsRepository.Get(DiscordGuildSetting.MainWorld, guildUser.Guild.Id.ToString()))?.Value;

            if (mainWorld != null && !worlds.Contains(mainWorld))
            {
                worlds.Add(mainWorld);
            }

            var userOwnedRolesMatchingWorlds = guildUser.Roles.Where(x => worlds.Contains(x.Name.ToLowerInvariant()) || x.Name.Equals(LinkedServerRole, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (!gw2AccountInfo.ValidKey)
            {
                await guildUser.SendMessageAsync($"{context ?? "Your"} api-key is invalid, please set a new one and re-verify.");
            }
            else if (gw2AccountInfo.TokenInfo.Name != (context ?? $"{guildUser.Username}#{guildUser.Discriminator}"))
            {
                await guildUser.SendMessageAsync(
                    $"{context ?? "You've"} most likely changed {(context != null ? "his/her" : "your")} discord username from {gw2AccountInfo.TokenInfo.Name} to {context ?? $"{guildUser.Username}#{guildUser.Discriminator}"}." +
                    $"\n{(context != null ? $"Please ask {context} to update his/her key." : "Please update your key.")}" +
                    "\nYou can view, create and edit your GW2 application key's on https://account.arena.net/applications");
            }
            else
            {
                if (gw2AccountInfo.WorldInfo != null && worlds.Contains(gw2AccountInfo.WorldInfo.Name.ToLowerInvariant()))
                {
                    await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, userOwnedRolesMatchingWorlds, gw2AccountInfo.WorldInfo.Name, bypassMessages, context);

                }
                else if (gw2AccountInfo.WorldInfo != null && gw2AccountInfo.WorldInfo.LinkedWorlds.Any(x => string.Equals(x.Name, mainWorld, StringComparison.InvariantCultureIgnoreCase)))
                {
                    await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, userOwnedRolesMatchingWorlds,
                        LinkedServerRole, bypassMessages, context);

                }
                else
                {
                    if (!bypassMessages)
                    {
                        if (gw2AccountInfo.WorldInfo == null)
                        {
                            await guildUser.SendMessageAsync($"Could not assign world roles on '{guildUser.Guild.Name}' for {context ?? "you"}.");
                        }
                        else
                        {
                            await guildUser.SendMessageAsync(
                                $"Your gw2 world does not belong to the verified worlds of '{guildUser.Guild.Name}' discord server, I can't assign {context ?? "your"} world role sorry!");
                        }
                    }
                }
            }

            await guildUser.RemoveRolesAsync(userOwnedRolesMatchingWorlds);
        }

        private async Task CreateRoleIfNotExistsAndAssignIfNeeded(SocketGuildUser guildUser, List<SocketRole> userOwnedRolesMatchingWorlds, string roleName, bool bypassMessages, string context = null)
        {
            var roleExistsAlready = userOwnedRolesMatchingWorlds.FirstOrDefault(x =>
                string.Equals(x.Name, roleName, StringComparison.InvariantCultureIgnoreCase));
            if (roleExistsAlready == null)
            {
                var cachedName = $"roles::{guildUser.Guild.Id}::{roleName}";
                var role = cache.GetFromCache(cachedName, TimeSpan.FromDays(1), () =>
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
                    await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, userOwnedRolesMatchingWorlds, roleName, bypassMessages, context);
                }
                if (!bypassMessages)
                {
                    await guildUser.SendMessageAsync($"{context ?? "You"} {(context != null ? "has" : "have")} been assigned role: {roleName} on {guildUser.Guild.Name}");
                }
            }
            userOwnedRolesMatchingWorlds.Remove(roleExistsAlready);
        }
    }
}
