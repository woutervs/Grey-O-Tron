using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.ApiClients;

namespace GreyOTron.Commands
{
    [Command("joke")]
    public class JokeCommand : ICommand
    {
        private readonly DadJokes _dadJokes;

        public JokeCommand(DadJokes dadJokes)
        {
            _dadJokes = dadJokes;
        }
        public async Task Execute(SocketMessage message)
        {
            await message.Channel.SendMessageAsync(await _dadJokes.GetJoke());
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }
        public string Arguments { get; set; }
    }
}