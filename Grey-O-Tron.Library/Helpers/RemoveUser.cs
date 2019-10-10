﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.TableStorage;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GreyOTron.Library.Helpers
{
    public class RemoveUser
    {
        private readonly KeyRepository gw2KeyRepository;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient log;
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;

        public RemoveUser(KeyRepository gw2KeyRepository, IConfiguration configuration, TelemetryClient log, DiscordGuildSettingsRepository discordGuildSettingsRepository)
        {
            this.gw2KeyRepository = gw2KeyRepository;
            this.configuration = configuration;
            this.log = log;
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
        }


        public async Task Execute(IUser user, IEnumerable<SocketGuild> guilds, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            await gw2KeyRepository.Delete("Gw2", user.Id.ToString());
            var affectedServers = new List<string>();
            foreach (var guild in guilds)
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

                    var guildUser = guild.Users.FirstOrDefault(u => u.Id == user.Id);
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
                    ExceptionHandler.HandleException(log, ex, user, $"Try delete roles in {guild.Name}.");
                }
            }
            await user.InternalSendMessageAsync($"Your gw2 world roles have been removed from {(affectedServers.Any() ? affectedServers.Aggregate("", (a, b) => $"{a}, {b} ").TrimEnd(',', ' ') : "no where")}.\nGoodbye!");
        }
    }
}