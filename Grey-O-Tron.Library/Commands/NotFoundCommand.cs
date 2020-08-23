using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Interfaces;
using GreyOTron.Resources;

namespace GreyOTron.Library.Commands
{
    public class NotFoundCommand : ICommand
    {
        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.CommandNotFound), Arguments);
        }
        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}