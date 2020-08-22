using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Services;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using UserExtensions = GreyOTron.Library.Extensions.UserExtensions;

namespace GreyOTron
{
    public class Bot
    {
        private readonly DiscordSocketClient client = new DiscordSocketClient();
        private readonly CommandProcessor processor;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient log;
        private readonly TimedExecutionsService timedExecutions;
        private readonly UserJoinedHelper userJoined;
        private CancellationToken cancellationToken;

        public Bot(CommandProcessor processor, IConfiguration configuration, TelemetryClient log, TimedExecutionsService timedExecutions, TranslationHelper translationHelper, UserJoinedHelper userJoined)
        {
            this.processor = processor;
            this.configuration = configuration;
            this.log = log;
            this.timedExecutions = timedExecutions;
            this.userJoined = userJoined;
            if (ulong.TryParse(configuration["OwnerId"], out var ownerId))
            {
                UserExtensions.OwnerId = ownerId;
            }

            UserExtensions.Log = log;
            UserExtensions.TranslationHelper = translationHelper;
        }

        public async Task Start(CancellationToken token)
        {
            cancellationToken = token;
            try
            {
                client.Ready += Ready;
                client.MessageReceived += ClientOnMessageReceived;
                client.Disconnected += ClientOnDisconnected;
                client.UserJoined += ClientOnUserJoined;

                var configurationTokenName = EnvironmentHelper.Is(Environments.Maintenance) ? "GreyOTron-TokenMaintenance" : "GreyOTron-Token";
                
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

        private async Task ClientOnUserJoined(SocketGuildUser joinedUser)
        {
            try
            {

                await userJoined.Execute(client, joinedUser, cancellationToken);
            }
            catch (Exception e)
            {
                ExceptionHandler.HandleException(client, log, e, joinedUser);
            }
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
                ExceptionHandler.HandleException(client, log, e, socketMessage.Author, socketMessage.Content);
            }
        }
    }
}
