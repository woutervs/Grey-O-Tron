using System;
using System.Diagnostics;
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
        private static IContainer container;
        private static DiscordSocketClient client;
        public static async Task Main()
        {
            container = AutofacConfiguration.Build();
            await Setup();
            AppDomain.CurrentDomain.UnhandledException += async (sender, args) =>
            {
                Trace.WriteLine(args.ExceptionObject.ToString());
                await Setup();
            };

            Console.ReadLine();
        }

        private static async Task Setup()
        {
            if (client != null)
            {
                try
                {
                    client.Dispose();
                    client = null;
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
            }
            var configuration = container.Resolve<IConfigurationRoot>();
            client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, configuration["GreyOTron-Token"]);
            await client.StartAsync();
            client.MessageReceived += ClientOnMessageReceived;
            await client.SetGameAsync($"{configuration["command-prefix"]}help");
            client.Disconnected += async exception =>
            {
                Trace.WriteLine(exception.Message);
                await Setup();
            };
        }

        private static async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            var processor = container.Resolve<CommandProcessor>();
            var command = processor.Parse(socketMessage.Content);
            await command.Execute(socketMessage);
        }
    }
}
