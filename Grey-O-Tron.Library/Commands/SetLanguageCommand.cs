using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Models;
using GreyOTron.Library.Services;
using GreyOTron.Resources;

namespace GreyOTron.Library.Commands
{
    [Command("set-language", CommandDescription = "Set your preferred language.", CommandArguments = "{language (en|fr|nl|de)}", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class SetLanguageCommand : ICommand
    {
        private readonly IDiscordUserRepository discordUserRepository;
        private readonly LanguagesService languages;
        
        public SetLanguageCommand(IDiscordUserRepository discordUserRepository, LanguagesService languages)
        {
            this.discordUserRepository = discordUserRepository;
            this.languages = languages;
        }

        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var language = Arguments.Trim().ToLowerInvariant();
            var languageExists = languages.Exists(language);
            if (languageExists)
            {
                await discordUserRepository.InsertOrUpdate(new DiscordUserDto
                    { Id = message.Author.Id, Username = message.Author.Username, Discriminator = message.Author.Discriminator, PreferredLanguage = language });
                languages.UpdateForUserId(message.Author.Id, language);
                await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.LanguageSet), language, nameof(GreyOTronResources.You));
            }
            else
            {
                await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.InvalidLanguage), language);
            }
            
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
