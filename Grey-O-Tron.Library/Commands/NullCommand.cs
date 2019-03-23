using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;

namespace GreyOTron.Library.Commands
{
    public class NullCommand : ICommand
    {
        public Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
        }
        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}