using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.ApplicationInsights;

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
            return user is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator;
        }

        public static string UserId(this IUser user)
        {
            return $"{user.Username}#{user.Discriminator}";
        }

        public static async Task InternalSendMessageAsync(this IUser user, string text)
        {
            try
            {
                await user.SendMessageAsync(text);
            }
            catch (Exception e)
            {
                var properties = new Dictionary<string, string> {{"UserId", user.UserId()}, {"Exception", e.Message}};
                if (user is SocketGuildUser guildUser)
                {
                    properties.Add("ServerName", guildUser.Guild.Name);
                    properties.Add("ServerId", guildUser.Guild.Id.ToString());
                    properties.Add("IsAdmin", guildUser.IsAdmin().ToString());
                    properties.Add("Roles", guildUser.Roles.Aggregate("", (s, role) => $"{s}{role.Name}, ", r => r.TrimEnd(' ', ',')));
                }
                Log?.TrackTrace("User can't receive message", properties);
            }
        }

        public static ulong? OwnerId { get; set; }
        public static TelemetryClient Log { get; set; }

        public static async Task SendMessageToBotOwner(this DiscordSocketClient client, string message)
        {
            if (OwnerId.HasValue)
            {
                await client.GetUser(OwnerId.Value).InternalSendMessageAsync(message);
            }
        }
    }
}
