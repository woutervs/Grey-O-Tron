using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Attributes;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Commands
{
    [Command("exception", CommandDescription = "Throws an exception", CommandOptions = CommandOptions.RequiresOwner)]
    public class ExceptionCommand : ICommand
    {
        public Task Execute(IMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
            throw new Exception("Exception command triggered.");
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
