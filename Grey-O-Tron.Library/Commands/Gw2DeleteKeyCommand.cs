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
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-remove-key", CommandDescription = "Removes Guild Wars 2 key from the database.", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class Gw2DeleteKeyCommand : ICommand
    {
        private readonly KeyRepository gw2KeyRepository;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient log;
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;

        public Gw2DeleteKeyCommand(KeyRepository gw2KeyRepository, IConfiguration configuration, TelemetryClient log, DiscordGuildSettingsRepository discordGuildSettingsRepository)
        {
            this.gw2KeyRepository = gw2KeyRepository;
            this.configuration = configuration;
            this.log = log;
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
        }

        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            await gw2KeyRepository.Delete("Gw2", message.Author.Id.ToString());
            List<string> affectedServers = new List<string>();
            foreach (var guild in Client.Guilds)
            {
                try
                {
                    var worlds = JsonConvert.DeserializeObject<List<string>>(
                        (await discordGuildSettingsRepository.Get(DiscordGuildSetting.Worlds, guild.Id.ToString()))
                        ?.Value ?? "[]");
                    var mainWorld =
                        (await discordGuildSettingsRepository.Get(DiscordGuildSetting.MainWorld, guild.Id.ToString()))
                        ?.Value;

                    if (mainWorld != null && !worlds.Contains(mainWorld))
                    {
                        worlds.Add(mainWorld);
                    }

                    var guildUser = guild.Users.FirstOrDefault(user => user.Id == message.Author.Id);
                    if (guildUser != null)
                    {
                        affectedServers.Add(guild.Name);
                        var userOwnedRolesMatchingWorlds = guildUser.Roles.Where(x =>
                            worlds.Contains(x.Name.ToLowerInvariant()) || x.Name.Equals(
                                configuration["LinkedServerRole"],
                                StringComparison.InvariantCultureIgnoreCase)).ToList();
                        await guildUser.RemoveRolesAsync(userOwnedRolesMatchingWorlds,
                            new RequestOptions { AuditLogReason = "User removed Gw2Api Key" });
                    }
                }
                catch (Exception ex)
                {
                    log.TrackException(ex, new Dictionary<string, string> { { "DiscordUser", $"{message.Author.Username}#{message.Author.Discriminator}" } });
                }
            }
            await message.Author.SendMessageAsync($"Your gw2 world roles have been removed from {(affectedServers.Any() ? affectedServers.Aggregate("", (a, b) => $"{a}, {b} ").TrimEnd(',', ' ') : "no where")}.\nGoodbye!");

            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
