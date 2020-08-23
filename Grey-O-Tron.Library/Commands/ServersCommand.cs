using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Attributes;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Commands
{
    [Command("servers", CommandDescription = "Returns all the severs where this bot is invited on.", CommandOptions = CommandOptions.RequiresOwner)]
    public class ServersCommand : ICommand
    {
        public async Task Execute(IMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var guilds = Client.Guilds.Aggregate("", (s, guild) => $"{s}{guild.Name}\n");
            await message.Author.InternalSendMessageAsync("Total: {0}\n{1}", Client.Guilds.Count.ToString(), guilds);
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
