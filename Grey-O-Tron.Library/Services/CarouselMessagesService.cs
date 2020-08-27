using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Services
{
    public class GameMessage
    {
        public string Message { get; set; }
        public string Url { get; set; }
        public ActivityType ActivityType { get; set; }
    }
    public class CarouselMessagesService
    {
        private readonly CarrouselHelper<GameMessage> messages;

        public CarouselMessagesService(IEnvironmentHelper environmentHelper)
        {
            switch (environmentHelper.Current)
            {
                case Environments.Development:
                    messages = new CarrouselHelper<GameMessage>(
                        new List<GameMessage>
                        {
                            new GameMessage  {ActivityType = ActivityType.Watching, Message = "dev database"}
                        });

                    break;
                case Environments.Maintenance:
                    messages = new CarrouselHelper<GameMessage>(
                        new List<GameMessage> {
                            new GameMessage {ActivityType = ActivityType.Playing, Message = "in maintenance mode"}
                        });
                    break;
                case Environments.Production:
                    messages = new CarrouselHelper<GameMessage>(
                        new List<GameMessage>
                        {
                            new GameMessage {ActivityType = ActivityType.Playing, Message = $"v{VersionResolverHelper.Get()}"},
                            new GameMessage {ActivityType = ActivityType.Watching, Url = "https://greyotron.eu",Message = "greyotron.eu"},
                            new GameMessage {ActivityType = ActivityType.Listening, Message = "got#help"},
                            new GameMessage {ActivityType = ActivityType.Streaming, Message = "how to assign a role", Url = "https://greyotron.eu/how-to/get-a-role"}
                        });

                    break;
            }
        }

        public async Task SetNextMessage(IDiscordClient client, CancellationToken token)
        {
            if (!token.IsCancellationRequested)
            {
                if (!(client is DiscordSocketClient socketClient))
                {
                    return;
                }

                var next = messages.Next();
                await socketClient.SetGameAsync(next.Message, null, next.ActivityType);
            }
        }
    }
}
