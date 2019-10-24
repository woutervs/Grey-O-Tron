using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace GreyOTron.Library.Helpers
{
    public class BotMessages
    {
        private readonly Carrousel messages;

        public BotMessages()
        {
            messages = new Carrousel(
                new List<string> {
                    $"v{VersionResolver.Get()}",
                    "greyotron.eu",
                    "got#help"
                });
#if MAINTENANCE
                messages = new Carrousel(
                new List<string> {
                   "MAINTENANCE MODE"
                });
#endif
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
