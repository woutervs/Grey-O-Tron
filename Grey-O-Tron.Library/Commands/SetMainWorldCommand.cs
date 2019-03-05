using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-set-main-world", CommandDescription = "Stores the discord server's main world to the database.", CommandArguments = "{world name}", CommandOptions = CommandOptions.DiscordServer | CommandOptions.RequiresAdmin)]
    public class SetMainWorldCommand : ICommand
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;
        private readonly Gw2Api gw2Api;

        public SetMainWorldCommand(DiscordGuildSettingsRepository discordGuildSettingsRepository, Gw2Api gw2Api)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
            this.gw2Api = gw2Api;
        }


        public async Task Execute(SocketMessage message)
        {
            if (message.Author is SocketGuildUser guildUser)
            {
                var world = gw2Api.ParseWorld(Arguments);

                if (world == null)
                {
                    await message.Author.SendMessageAsync($"Could not resolve your world from '{Arguments}'");
                }
                else if (guildUser.GuildPermissions.Administrator || guildUser.Id == 188365172757233664)
                {
                    await discordGuildSettingsRepository.Clear(DiscordGuildSetting.MainWorld, guildUser.Guild.Id.ToString());
                    await discordGuildSettingsRepository.Set(new DiscordGuildSetting(guildUser.Guild.Id.ToString(), guildUser.Guild.Name, DiscordGuildSetting.MainWorld,
                        world.Name.ToLowerInvariant()));
                    if (!(await discordGuildSettingsRepository.Get(DiscordGuildSetting.World, guildUser.Guild.Id.ToString())).Any(x => x.Value.Equals(world.Name, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        await discordGuildSettingsRepository.Set(new DiscordGuildSetting(guildUser.Guild.Id.ToString(), guildUser.Guild.Name, DiscordGuildSetting.World, world.Name.ToLowerInvariant()));
                    }

                    await guildUser.SendMessageAsync($"{world.Name} set for {guildUser.Guild.Name} as main world.");
                }
                else
                {
                    await guildUser.SendMessageAsync(
                        "You must have administrative permissions to perform the set-worlds command.");
                }
            }
            else
            {
                await message.Author.SendMessageAsync(
                    "The set-worlds command must be used from within the server to which you want to apply it.");
            }
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }

        public string Arguments { get; set; }
    }
}
