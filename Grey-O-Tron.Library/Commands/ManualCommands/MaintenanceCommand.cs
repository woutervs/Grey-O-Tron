using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Commands.ManualCommands
{
    public class MaintenanceCommand : ICommand
    {
        public async Task Execute(IMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await message.Author.SendMessageAsync("Currently in maintenance mode, can't process commands.");
            await Task.CompletedTask;
        }
        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}