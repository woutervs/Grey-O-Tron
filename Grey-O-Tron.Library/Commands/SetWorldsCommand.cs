using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Models;
using GreyOTron.Library.RepositoryInterfaces;
using GreyOTron.Resources;
using Newtonsoft.Json;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-set-worlds", CommandDescription = "Stores worlds where roles will be assigned for to the database.", CommandArguments = "{world (name|id);world (name|id);...}|{all}", CommandOptions = CommandOptions.DiscordServer | CommandOptions.RequiresAdmin)]
    public class SetWorldsCommand : ICommand
    {
        private readonly IGw2DiscordServerRepository gw2DiscordServerRepository;
        private readonly Gw2Api gw2Api;

        public SetWorldsCommand(IGw2DiscordServerRepository gw2DiscordServerRepository, Gw2Api gw2Api)
        {
            this.gw2DiscordServerRepository = gw2DiscordServerRepository;
            this.gw2Api = gw2Api;
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
                    await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.NoWorldsInCommand));
                }
                else if (guildUser.IsAdminOrOwner())
                {
                    var gw2DiscordServer = new Gw2DiscordServer
                    {
                        DiscordServer = new DiscordServerDto
                        {
                            Id = guildUser.Guild.Id,
                            Name = guildUser.Guild.Name,
                        },
                        Worlds = worlds.Select(x => new Gw2WorldDto { Id = x.Id }).ToList()
                    };
                    await gw2DiscordServerRepository.InsertOrUpdate(gw2DiscordServer);

                    await guildUser.InternalSendMessageAsync(nameof(GreyOTronResources.WorldsSetForGuild), worlds.Aggregate("", (a, b) => $"{a}{b.Name}, ").TrimEnd(',', ' '), guildUser.Guild.Name);
                }
                else
                {
                    await guildUser.InternalSendMessageAsync(nameof(GreyOTronResources.AdministrativePermissionsOnly), "gw2-set-worlds");
                }
            }
            else
            {
                await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.ServerOnlyCommand), "gw2-set-worlds");
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
