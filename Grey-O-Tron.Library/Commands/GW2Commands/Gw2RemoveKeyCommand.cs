using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Attributes;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using Microsoft.ApplicationInsights;

namespace GreyOTron.Library.Commands.GW2Commands
{
    [Command("gw2-remove-key", CommandDescription = "Removes Guild Wars 2 key from the database.", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class Gw2RemoveKeyCommand : ICommand
    {
        private readonly RemoveUserHelper removeUser;

        public Gw2RemoveKeyCommand(RemoveUserHelper removeUser)
        {
            this.removeUser = removeUser;
        }

        public async Task Execute(IMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await removeUser.Execute(Client, message.Author, Client.Guilds, cancellationToken);
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
