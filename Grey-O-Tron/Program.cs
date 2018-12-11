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
            Environment.Exit(-1);
        }

        private static async Task Setup()
        {
            Log.TrackTrace("Bot started.");
            var configuration = container.Resolve<IConfigurationRoot>();
            client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, configuration["GreyOTron-Token"]);
            await client.StartAsync();
            await client.SetGameAsync($"{configuration["command-prefix"]}help | greyotron.eu");

            var isLoggedIn = false;
            client.LoggedIn += async () =>
            {
                isLoggedIn = true;
                await Task.CompletedTask;
            };

            client.MessageReceived += ClientOnMessageReceived;

            while (isLoggedIn == false || client.ConnectionState == ConnectionState.Connecting || client.ConnectionState == ConnectionState.Connected)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
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
                Log.TrackTrace($"{socketMessage.Author.Username}#{socketMessage.Author.Discriminator}");
                Log.TrackTrace(socketMessage.Content);
                Log.TrackException(e);
            }

        }
    }
}
