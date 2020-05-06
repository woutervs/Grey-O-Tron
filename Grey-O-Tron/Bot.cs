using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Helpers;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using UserExtensions = GreyOTron.Library.Helpers.UserExtensions;

namespace GreyOTron
{
    public class Bot
    {
        private readonly DiscordSocketClient client = new DiscordSocketClient();
        private readonly CommandProcessor processor;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient log;
        private readonly TimedExecutions timedExecutions;
        private CancellationToken cancellationToken;

        public Bot(CommandProcessor processor, IConfiguration configuration, TelemetryClient log, TimedExecutions timedExecutions)
        {
            this.processor = processor;
            this.configuration = configuration;
            this.log = log;
            this.timedExecutions = timedExecutions;
            if (ulong.TryParse(configuration["OwnerId"], out var ownerId))
            {
                UserExtensions.OwnerId = ownerId;
            }

            UserExtensions.Log = log;
            //TODO: UserExtensions.TranslationService ...
        }

        public async Task Start(CancellationToken token)
        {
            cancellationToken = token;
            try
            {
                client.Ready += Ready;
                client.MessageReceived += ClientOnMessageReceived;
                client.Disconnected += ClientOnDisconnected;

#if MAINTENANCE
                const string configurationTokenName = "GreyOTron-TokenMaintenance";
#else
                const string configurationTokenName = "GreyOTron-Token";
#endif
                await client.LoginAsync(TokenType.Bot, configuration[configurationTokenName]);

                await client.StartAsync();

                log.TrackTrace("Bot started.");
            }
            catch (Exception ex)
            {
                log.TrackException(ex);
            }

            await timedExecutions.Setup(client);

            await Task.Delay(-1, cancellationToken);
        }

        private async Task ClientOnDisconnected(Exception arg)
        {
            log.TrackException(arg, new Dictionary<string, string> { { "section", "ClientOnDisconnected" } });
            await Task.CompletedTask;
            //await timedExecutions.Stop();
        }

        public async Task Stop()
        {
            client.Ready -= Ready;
            client.MessageReceived -= ClientOnMessageReceived;
            client.Disconnected -= ClientOnDisconnected;
            try
            {
                await timedExecutions.Stop();
                await client.LogoutAsync();
                await client.StopAsync();

            }
            catch (Exception e)
            {
                log.TrackException(e);
            }
            finally
            {
                log.TrackTrace("Bot stopped.");
            }
        }

        private async Task Ready()
        {
            log.TrackTrace("Bot ready.");
            await timedExecutions.Start();
        }

        private async Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            try
            {
                var command = processor.Parse(socketMessage.Content);
                command.Client = client;
                await command.Execute(socketMessage, cancellationToken);
            }
            catch (Exception e)
            {
                ExceptionHandler.HandleException(log, e, socketMessage.Author, socketMessage.Content);
            }
        }
    }
}
