using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Commands.ManualCommands
{
    public class NullCommand : ICommand
    {
        public Task Execute(IMessage message, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
        }
        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}