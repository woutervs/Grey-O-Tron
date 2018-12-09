using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Discord;
using Discord.WebSocket;
using GreyOTron.Commands;
using GreyOTron.Helpers;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;

namespace GreyOTron
{
    public class Program
    {
        private static IContainer container;
        private static DiscordSocketClient client;
        private static readonly TelemetryClient Log = new TelemetryClient();
        public static async Task Main()
        {
            container = AutofacConfiguration.Build();
            await Setup();
            Console.ReadLine();
        }

        private static async Task Setup()
        {
            Log.TrackTrace("Bot started.");
            Trace.WriteLine("Bot started.");
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
            Trace.WriteLine("Configuration loaded.");
            client = new DiscordSocketClient();
            Trace.WriteLine("Logging in.");
            await client.LoginAsync(TokenType.Bot, configuration["GreyOTron-Token"]);
            await client.StartAsync();
            await client.SetGameAsync($"{configuration["command-prefix"]}help | greyotron.eu");
            Trace.WriteLine("Start + SetGameAsync");

            client.Disconnected += async exception =>
            {
                Trace.WriteLine(exception.Message);
                await Setup();
            };

            client.MessageReceived += ClientOnMessageReceived;
        }

        private static async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            try
            {
                var processor = container.Resolve<CommandProcessor>();
                var command = processor.Parse(socketMessage.Content);
                await command.Execute(socketMessage);
            }
            catch (Exception e)
            {
                Log.TrackException(e);
            }

        }
    }
}
