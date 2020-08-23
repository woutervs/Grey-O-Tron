using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Services
{
    public class CarouselMessagesService
    {
        private readonly CarrouselHelper messages;

        public CarouselMessagesService(IEnvironmentHelper environmentHelper)
        {
            switch (environmentHelper.Current)
            {
                case Environments.Development:
                    messages = new CarrouselHelper(
                        new List<string>
                        {
                            "DEV MODE ON LOCALDB",
                        });

                    break;
                case Environments.Maintenance:
                    messages = new CarrouselHelper(
                        new List<string> {
                            "MAINTENANCE MODE"
                        });
                    break;
                case Environments.Production:
                    messages = new CarrouselHelper(
                        new List<string>
                        {
                            $"v{VersionResolverHelper.Get()}",
                            "greyotron.eu",
                            "got#help"
                        });

                    break;
            }
        }

        public async Task SetNextMessage(DiscordSocketClient client, CancellationToken token)
        {
            if (!token.IsCancellationRequested)
            {
                await client.SetGameAsync(messages.Next());
            }
        }
    }
}
