using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;

namespace GreyOTron.Library.Commands
{
    public class MaintenanceCommand : ICommand
    {
        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            await message.Author.SendMessageAsync("Currently in maintenance mode, can't process commands.");
            await Task.CompletedTask;
        }
        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}