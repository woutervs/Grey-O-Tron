using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Attributes;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Commands
{
    [Command("joke", CommandDescription = "Will tell a joke.", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class JokeCommand : ICommand
    {
        private readonly DadJokes dadJokes;

        public JokeCommand(DadJokes dadJokes)
        {
            this.dadJokes = dadJokes;
        }

        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await message.Channel.SendMessageAsync(await dadJokes.GetJoke());
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }
        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}