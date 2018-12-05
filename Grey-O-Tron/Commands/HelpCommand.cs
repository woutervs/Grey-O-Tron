using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Commands
{
    [Command("help")]
    public class HelpCommand : ICommand
    {
        private readonly IConfigurationRoot _configuration;

        public HelpCommand(IConfigurationRoot configuration)
        {
            _configuration = configuration;
        }

        public async Task Execute(SocketMessage message)
        {
            await message.Author.SendMessageAsync($"Currently I know the following commands:" +
                                                        $"\n\n**{_configuration["command-prefix"]}joke**" +
                                                        $"\n\n**{_configuration["command-prefix"]}gw2-key key**\nYour GW2 application key can be set on https://account.arena.net/applications as name you have to use your discord username: **{message.Author.Username}#{message.Author.Discriminator}**" +
                                                        $"\n\n**{_configuration["command-prefix"]}gw2-verify**\nThis will verify your previously stored GW2 application key and set your roles accordingly on the discord server you've invoked this command on." +
                                                        $"\n\n**{_configuration["command-prefix"]}set-worlds worldname;otherworldname**\nYou must have administrative permissions to perform this command.");
            await message.Channel.DeleteMessagesAsync(new List<SocketMessage> { message });
        }
        public string Arguments { get; set; }
    }
}
