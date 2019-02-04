using System.Threading.Tasks;
using Discord.WebSocket;

namespace GreyOTron.Library.Commands
{
    public class NullCommand : ICommand
    {
        public Task Execute(SocketMessage message)
        {
            return Task.CompletedTask;
        }
        public string Arguments { get; set; }
    }
}