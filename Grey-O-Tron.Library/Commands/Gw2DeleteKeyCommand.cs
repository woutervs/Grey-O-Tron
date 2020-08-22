using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-remove-key", CommandDescription = "Removes Guild Wars 2 key from the database.", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class Gw2DeleteKeyCommand : ICommand
    {
        private readonly RemoveUserHelper removeUser;

        public Gw2DeleteKeyCommand(RemoveUserHelper removeUser)
        {
            this.removeUser = removeUser;
        }

        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await removeUser.Execute(Client, message.Author, Client.Guilds, cancellationToken);

            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
