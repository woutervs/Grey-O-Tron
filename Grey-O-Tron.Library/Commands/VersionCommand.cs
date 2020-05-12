using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Translations;

namespace GreyOTron.Library.Commands
{
    [Command("version", CommandDescription = "Get the current bot version.", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class VersionCommand : ICommand
    {
        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.CurrentVersion), VersionResolver.Get());
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
