using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using GreyOTron.Resources;

namespace GreyOTron.Library.Commands
{
    [Command("help", CommandDescription = "The help command that directs you to this very page.", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class HelpCommand : ICommand
    {
        private readonly CommandResolverHelper resolver;
        public HelpCommand(CommandResolverHelper resolver)
        {
            this.resolver = resolver;
        }
        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var resolverCommands = resolver.Commands;
            if (!message.Author.IsOwner())
            {
                resolverCommands = resolverCommands.Where(x => !x.Options.HasFlag(CommandOptions.RequiresOwner));
            }

            if (!message.Author.IsAdmin())
            {
                resolverCommands = resolverCommands.Where(x => !x.Options.HasFlag(CommandOptions.RequiresAdmin));
            }
            await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.HelpMessage), $"{resolverCommands.Aggregate("", (s, command) => $"{s} {command}\n")}");


            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }
        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
