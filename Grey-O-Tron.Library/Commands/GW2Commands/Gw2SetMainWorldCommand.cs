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
    [Command("gw2-set-main-world", CommandDescription = "Stores the discord server's main world to the database.", CommandArguments = "{world (name|id)}", CommandOptions = CommandOptions.DiscordServer | CommandOptions.RequiresAdmin)]
    public class Gw2SetMainWorldCommand : ICommand
    {
        private readonly IGw2DiscordServerRepository gw2DiscordServerRepository;
        private readonly Gw2Api gw2Api;

        public Gw2SetMainWorldCommand(IGw2DiscordServerRepository gw2DiscordServerRepository, Gw2Api gw2Api)
        {
            this.gw2DiscordServerRepository = gw2DiscordServerRepository;
            this.gw2Api = gw2Api;
        }


        public async Task Execute(IMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var guildUser = (IGuildUser)message.Author;
            var world = gw2Api.ParseWorld(Arguments);

            if (world == null)
            {
                await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.UnknownWorld), Arguments);
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
                    MainWorld = new Gw2WorldDto { Id = world.Id, Name = world.Name }
                };
                await gw2DiscordServerRepository.InsertOrUpdate(gw2DiscordServer);

                await guildUser.InternalSendMessageAsync(nameof(GreyOTronResources.MainWorldSet), world.Name,
                    guildUser.Guild.Name);
            }
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
