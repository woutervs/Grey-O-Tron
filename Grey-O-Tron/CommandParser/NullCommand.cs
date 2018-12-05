using System.Threading.Tasks;
using Discord.WebSocket;

namespace GreyOTron.CommandParser
{
    public class NullCommand : ICommand
    {
        public Task Execute(SocketMessage message)
        {
            return Task.CompletedTask;
        }

        public string Name { get; } = "";
        public string Arguments { get; set; }
    }
}