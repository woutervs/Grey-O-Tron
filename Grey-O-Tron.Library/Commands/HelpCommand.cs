using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Helpers;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Library.Commands
{
    [Command("help")]
    public class HelpCommand : ICommand
    {
        private readonly IConfiguration configuration;

        public HelpCommand(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task Execute(SocketMessage message)
        {
            await message.Author.SendMessageAsync($"Currently I know the following commands:" +
                                                        $"\n\n**{configuration["command-prefix"]}joke**" +
                                                        $"\n\n**{configuration["command-prefix"]}gw2-key key**\nYour GW2 application key can be set on https://account.arena.net/applications as name you have to use your discord username: **{message.Author.Username}#{message.Author.Discriminator}**" +
                                                        $"\n\n**{configuration["command-prefix"]}gw2-verify**\nThis will verify your previously stored GW2 application key and set your roles accordingly on the discord server you've invoked this command on." +
                                                        $"\n\n**{configuration["command-prefix"]}gw2-set-worlds worldname;otherworldname**\nYou must have administrative permissions to perform this command.");
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }
        public string Arguments { get; set; }
    }
}
