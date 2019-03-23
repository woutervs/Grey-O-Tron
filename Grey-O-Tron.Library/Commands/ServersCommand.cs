using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Library.Commands
{
    [Command("servers", CommandDescription = "Returns all the severs where this bot is invited on.", CommandOptions = CommandOptions.RequiresOwner)]
    public class ServersCommand : ICommand
    {
        private readonly IConfiguration configuration;

        public ServersCommand(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (message.Author.Id == ulong.Parse(configuration["OwnerId"]))
            {
                var guilds = Client.Guilds.Aggregate("", (s, guild) => $"{s}{guild.Name}\n");
                await message.Author.SendMessageAsync($"Total: {Client.Guilds.Count}\n{guilds}");
            }
            else
            {
                await message.Author.SendMessageAsync("I'm sorry you're not authorized to receive this kind of information.");
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
