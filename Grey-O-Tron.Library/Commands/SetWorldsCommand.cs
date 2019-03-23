using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-set-worlds", CommandDescription = "Stores worlds where roles will be assigned for to the database.", CommandArguments = "{world (name|id);world (name|id);...}|{all}", CommandOptions = CommandOptions.DiscordServer | CommandOptions.RequiresAdmin)]
    public class SetWorldsCommand : ICommand
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;
        private readonly Gw2Api gw2Api;
        private readonly IConfiguration configuration;

        public SetWorldsCommand(DiscordGuildSettingsRepository discordGuildSettingsRepository, Gw2Api gw2Api, IConfiguration configuration)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
            this.gw2Api = gw2Api;
            this.configuration = configuration;
        }

        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (message.Author is SocketGuildUser guildUser)
            {
                List<World> worlds;
                if (Arguments.Equals("all", StringComparison.InvariantCultureIgnoreCase))
                {
                    worlds = gw2Api.GetWorlds().ToList();
                }
                else
                {

                    worlds = Arguments.TrimEnd(';', ',').Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(gw2Api.ParseWorld).Where(x => x != null).Distinct().ToList();
                }

                if (!worlds.Any())
                {
                    await message.Author.SendMessageAsync(
                        "You must give at least one world name separated by ; for the set-worlds command to work.");
                }
                else if (guildUser.GuildPermissions.Administrator || guildUser.Id == ulong.Parse(configuration["OwnerId"]))
                {
                    await discordGuildSettingsRepository.Set(new DiscordGuildSetting(guildUser.Guild.Id.ToString(),
                        guildUser.Guild.Name, DiscordGuildSetting.Worlds,
                        JsonConvert.SerializeObject(worlds.Select(x => x.Name.ToLowerInvariant()))));
                    await guildUser.SendMessageAsync(
                        $"{worlds.Aggregate("", (a, b) => $"{a}{b.Name}, ").TrimEnd(',', ' ')} set for {guildUser.Guild.Name}");
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
        public DiscordSocketClient Client { get; set; }
    }
}
