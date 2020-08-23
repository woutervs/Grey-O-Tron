using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace GreyOTron.Library.Interfaces
{
    public interface ICommand
    {
        Task Execute(IMessage message, CancellationToken cancellationToken);
        string Arguments { get; set; }
        DiscordSocketClient Client { get; set; }
    }
}
