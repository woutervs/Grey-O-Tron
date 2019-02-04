using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.TableStorage;

namespace GreyOTron.Library.Helpers
{
    public class VerifyUser
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;
        private const string LinkedServerRole = "Linked Server";

        public VerifyUser(DiscordGuildSettingsRepository discordGuildSettingsRepository)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
        }

        public async Task Verify(AccountInfo gw2AccountInfo, SocketGuildUser guildUser, bool bypassNotBelongingMessage = false)
        {
            var worlds =
                (await discordGuildSettingsRepository.Get(DiscordGuildSetting.World, guildUser.Guild.Id.ToString())).Select(x =>
                    x.Value);
            var mainWorld = (await discordGuildSettingsRepository.Get(DiscordGuildSetting.MainWorld, guildUser.Guild.Id.ToString())).Select(x =>
                x.Value).FirstOrDefault();

            var userOwnedRolesMatchingWorlds = guildUser.Roles.Where(x => worlds.Contains(x.Name.ToLowerInvariant()) || x.Name.Equals(LinkedServerRole, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (gw2AccountInfo.WorldInfo != null && worlds.Contains(gw2AccountInfo.WorldInfo.Name.ToLowerInvariant()))
            {
                await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, userOwnedRolesMatchingWorlds, gw2AccountInfo.WorldInfo.Name);

            }
            else if (gw2AccountInfo.WorldInfo != null && gw2AccountInfo.WorldInfo.LinkedWorlds.Any(x => string.Equals(x.Name, mainWorld, StringComparison.InvariantCultureIgnoreCase)))
            {
                await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, userOwnedRolesMatchingWorlds,
                    LinkedServerRole);

            }
            else
            {
                if (!bypassNotBelongingMessage)
                {
                    await guildUser.SendMessageAsync(
                        $"Your gw2 world does not belong to the verified worlds of '{guildUser.Guild.Name}' discord server, I can't assign your world role sorry!");
                }
            }
            await guildUser.RemoveRolesAsync(userOwnedRolesMatchingWorlds);
        }

        private async Task CreateRoleIfNotExistsAndAssignIfNeeded(SocketGuildUser guildUser, List<SocketRole> userOwnedRolesMatchingWorlds, string roleName)
        {
            var roleExistsAlready = userOwnedRolesMatchingWorlds.FirstOrDefault(x =>
                string.Equals(x.Name, roleName, StringComparison.InvariantCultureIgnoreCase));
            if (roleExistsAlready == null)
            {
                var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Name == roleName);


                if (role == null)
                {
                    var restRole =
                        await guildUser.Guild.CreateRoleAsync(roleName, GuildPermissions.None);
                    await guildUser.AddRoleAsync(restRole);
                }
                else
                {
                    await guildUser.AddRoleAsync(role);
                }

                await guildUser.SendMessageAsync($"You have been assigned role: {roleName} on {guildUser.Guild.Name}");
            }
            userOwnedRolesMatchingWorlds.Remove(roleExistsAlready);
        }
    }
}
