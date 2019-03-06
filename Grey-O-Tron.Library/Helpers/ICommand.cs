using System.Threading.Tasks;
using Discord.WebSocket;

namespace GreyOTron.Library.Helpers
{
    public interface ICommand
    {
        Task Execute(SocketMessage message);
        string Arguments { get; set; }
        DiscordSocketClient Client { get; set; }
    }
}
