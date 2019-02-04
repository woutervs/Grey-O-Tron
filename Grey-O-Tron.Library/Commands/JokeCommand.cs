using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;

namespace GreyOTron.Library.Commands
{
    [Command("joke")]
    public class JokeCommand : ICommand
    {
        private readonly DadJokes dadJokes;

        public JokeCommand(DadJokes dadJokes)
        {
            this.dadJokes = dadJokes;
        }
        public async Task Execute(SocketMessage message)
        {
            await message.Channel.SendMessageAsync(await dadJokes.GetJoke());
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }
        public string Arguments { get; set; }
    }
}