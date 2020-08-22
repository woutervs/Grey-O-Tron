using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using GreyOTron.Resources;

namespace GreyOTron.Library.Commands
{
    [Command("servers", CommandDescription = "Returns all the severs where this bot is invited on.", CommandOptions = CommandOptions.RequiresOwner)]
    public class ServersCommand : ICommand
    {
        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            if (message.Author.IsOwner())
            {
                var guilds = Client.Guilds.Aggregate("", (s, guild) => $"{s}{guild.Name}\n");
                await message.Author.InternalSendMessageAsync("Total: {0}\n{1}", Client.Guilds.Count.ToString(), guilds);
            }
            else
            {
                await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.Unauthorized));
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
