﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.ApiClients;

namespace GreyOTron.CommandParser
{
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
            await message.Channel.DeleteMessagesAsync(new List<SocketMessage> { message });
        }

        public string Name { get; } = "joke";
        public string Arguments { get; set; }
    }
}