using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Attributes;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Commands
{
    [Command("exception", CommandDescription = "Throws an exception", CommandOptions = CommandOptions.RequiresOwner)]
    public class ExceptionCommand : ICommand
    {
        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
            if (message.Author.IsOwner())
            {
                throw new Exception("Exception command triggered.");
            }
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
