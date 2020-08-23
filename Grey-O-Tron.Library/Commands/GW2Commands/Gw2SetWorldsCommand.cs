using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Attributes;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Models;
using GreyOTron.Resources;

namespace GreyOTron.Library.Commands.GW2Commands
{
    [Command("gw2-set-worlds", CommandDescription = "Stores worlds where roles will be assigned for to the database.", CommandArguments = "{world (name|id);world (name|id);...}|{all}", CommandOptions = CommandOptions.DiscordServer | CommandOptions.RequiresAdmin)]
    public class Gw2SetWorldsCommand : ICommand
    {
        private readonly IGw2DiscordServerRepository gw2DiscordServerRepository;
        private readonly Gw2Api gw2Api;

        public Gw2SetWorldsCommand(IGw2DiscordServerRepository gw2DiscordServerRepository, Gw2Api gw2Api)
        {
            this.gw2DiscordServerRepository = gw2DiscordServerRepository;
            this.gw2Api = gw2Api;
        }

        public async Task Execute(IMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var guildUser = (IGuildUser)message.Author;
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
            else
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
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
