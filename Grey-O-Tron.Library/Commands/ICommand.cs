using System.Threading.Tasks;
using Discord.WebSocket;

namespace GreyOTron.Library.Commands
{
    public interface ICommand
    {
        Task Execute(SocketMessage message);
        string Arguments { get; set; }
    }
}
