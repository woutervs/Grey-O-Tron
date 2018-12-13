using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.TableStorage;

namespace GreyOTron.Commands
{
    [Command("gw2-set-worlds")]
    public class SetWorldsCommand : ICommand
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;

        public SetWorldsCommand(DiscordGuildSettingsRepository discordGuildSettingsRepository)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
        }

        public async Task Execute(SocketMessage message)
        {
            var worlds = Arguments.TrimEnd(';', ',').Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (!worlds.Any())
            {
                await message.Author.SendMessageAsync(
                    "You must give at least one world name seperated by ; for the set-worlds command to work.");
            }
            else
            {
                if (message.Author is SocketGuildUser guildUser)
                {
                    if (guildUser.GuildPermissions.Administrator)
                    {
                        await discordGuildSettingsRepository.Clear(DiscordGuildSetting.World, guildUser.Guild.Id.ToString());
                        foreach (var world in worlds)
                        {
                            await discordGuildSettingsRepository.Set(new DiscordGuildSetting(guildUser.Guild.Id.ToString(),guildUser.Guild.Name,DiscordGuildSetting.World,
                                world.ToLowerInvariant()));
                            await guildUser.SendMessageAsync($"{world} set for {guildUser.Guild.Name}");
                        }
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
