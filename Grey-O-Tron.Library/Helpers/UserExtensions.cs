using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace GreyOTron.Library.Helpers
{
    public static class UserExtensions
    {
        public static bool IsAdminOrOwner(this IUser user)
        {
            return user.IsOwner() || user.IsAdmin();
        }

        public static bool IsOwner(this IUser user)
        {
            return OwnerId.HasValue && user.Id == OwnerId;
        }

        public static bool IsAdmin(this IUser user)
        {
            return user is SocketGuildUser guildUser && guildUser.IsAdmin();
        }

        public static ulong? OwnerId { get; set; }

        public static async Task SendMessageToBotOwner(this DiscordSocketClient client, string message)
        {
            if (OwnerId.HasValue)
            {
                await client.GetUser(OwnerId.Value).SendMessageAsync(message);
            }
        }
    }
}
