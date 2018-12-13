using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.ApiClients;
using GreyOTron.TableStorage;

namespace GreyOTron.Helpers
{
    public class VerifyUser
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;

        public VerifyUser(DiscordGuildSettingsRepository discordGuildSettingsRepository)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
        }

        public async Task Verify(AccountInfo gw2AccountInfo, SocketGuildUser guildUser)
        {
            var worlds =
                (await discordGuildSettingsRepository.Get(DiscordGuildSetting.World, guildUser.Guild.Id.ToString())).Select(x =>
                    x.RowKey);
            if (gw2AccountInfo.WorldInfo != null && worlds.Contains(gw2AccountInfo.WorldInfo.Name.ToLowerInvariant()))
            {
                var role = guildUser.Guild.Roles.FirstOrDefault(x => x.Name == gw2AccountInfo.WorldInfo.Name);


                if (role == null)
                {
                    var restRole =
                        await guildUser.Guild.CreateRoleAsync(gw2AccountInfo.WorldInfo.Name, GuildPermissions.None);
                    await guildUser.AddRoleAsync(restRole);
                }
                else
                {
                    await guildUser.AddRoleAsync(role);
                }
                await guildUser.SendMessageAsync($"You have been assigned role: {role} on {guildUser.Guild.Name}");
            }
            else
            {
                await guildUser.SendMessageAsync("Your gw2 key does not belong to the verified worlds of this discord server, I can't assign your world role sorry!");
            }
        }
    }
}
