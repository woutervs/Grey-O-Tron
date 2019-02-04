using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;

namespace GreyOTron.Library.Commands
{
    [Command("version")]
    public class VersionCommand : ICommand
    {
        public async Task Execute(SocketMessage message)
        {
            await message.Author.SendMessageAsync($"Current version: {VersionResolver.Get()}");
        }

        public string Arguments { get; set; }
    }
}
