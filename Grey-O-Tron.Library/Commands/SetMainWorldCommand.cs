using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-set-main-world", CommandDescription = "Stores the discord server's main world to the database.", CommandArguments = "{world name}", CommandOptions = CommandOptions.DiscordServer | CommandOptions.RequiresAdmin)]
    public class SetMainWorldCommand : ICommand
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;
        public SetMainWorldCommand(DiscordGuildSettingsRepository discordGuildSettingsRepository)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
        }


        public async Task Execute(SocketMessage message)
        {
            if (string.IsNullOrEmpty(Arguments))
            {
                await message.Author.SendMessageAsync("World cannot be empty.");
            }
            else
            {
                if (message.Author is SocketGuildUser guildUser)
                {
                    if (guildUser.GuildPermissions.Administrator || guildUser.Id == 188365172757233664)
                    {
                        await discordGuildSettingsRepository.Clear(DiscordGuildSetting.MainWorld, guildUser.Guild.Id.ToString());
                        await discordGuildSettingsRepository.Set(new DiscordGuildSetting(guildUser.Guild.Id.ToString(), guildUser.Guild.Name, DiscordGuildSetting.MainWorld,
                            Arguments.ToLowerInvariant()));
                        await guildUser.SendMessageAsync($"{Arguments} set for {guildUser.Guild.Name} as main world.");
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
            }
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }

        public string Arguments { get; set; }
    }
}
