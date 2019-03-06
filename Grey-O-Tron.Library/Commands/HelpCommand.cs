using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;

namespace GreyOTron.Library.Commands
{
    [Command("help", CommandDescription = "The help command that directs you to this very page.", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class HelpCommand : ICommand
    {
        public async Task Execute(SocketMessage message)
        {
            await message.Author.SendMessageAsync("My commands can be found on: https://greyotron.eu/commands");
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }
        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
