using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;

namespace GreyOTron.Library.Commands
{
    public class NotFoundCommand : ICommand
    {
        public async Task Execute(SocketMessage message)
        {
            await message.Author.SendMessageAsync($"You tried using '{Arguments}', unfortunately I haven't been taught that command.");
        }
        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}