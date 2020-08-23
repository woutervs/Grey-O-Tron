using FluentAssertions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Services;
using Xunit;

namespace GreyOTron.Library.Tests
{
    public class LanguageServiceTests
    {
        [Theory]
        [GreyOTronLibraryAutoData]
        public void TestAvailableLanguages(IDiscordUserRepository discordUserRepository, IDiscordServerRepository discordServerRepository)
        {
            var languageService = new LanguagesService(new CacheHelper(), discordUserRepository, discordServerRepository);
            languageService.AvailableLanguages.Should().NotBeEmpty();
        }
    }
}
