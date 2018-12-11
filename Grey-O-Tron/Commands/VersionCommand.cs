using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Helpers;

namespace GreyOTron.Commands
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
