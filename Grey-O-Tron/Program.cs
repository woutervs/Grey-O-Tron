using System;
using System.Threading.Tasks;
using Autofac;
using Discord;
using Discord.WebSocket;
using GreyOTron.Commands;
using GreyOTron.Helpers;
using Microsoft.Extensions.Configuration;

namespace GreyOTron
{
    public class Program
    {
        private static DiscordSocketClient client;
        private static CommandProcessor processor;
        public static async Task Main()
        {
            var container = AutofacConfiguration.Build();
            processor = container.Resolve<CommandProcessor>();

            var configuration = container.Resolve<IConfigurationRoot>();
            client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, configuration["GreyOTron-Token"]);
            await client.StartAsync();
            client.MessageReceived += ClientOnMessageReceived;
            await client.SetGameAsync($"{configuration["command-prefix"]}help");

            Console.ReadLine();
        }
        private static async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            var command = processor.Parse(socketMessage.Content);
            await command.Execute(socketMessage);
        }
    }
}
