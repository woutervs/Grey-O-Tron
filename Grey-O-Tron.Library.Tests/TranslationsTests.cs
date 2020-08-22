using GreyOTron.Library.Helpers;
using Xunit;
using FluentAssertions;
using GreyOTron.Library.Services;

namespace Grey_O_Tron.Library.Tests
{
    public class TranslationsTests
    {
        [Fact]
        public void Test_TranslationIssues()
        {
            var t = new TranslationHelper(new LanguagesService(new CacheHelper(), null, null));
            var result = t.Translate(0, null, "{0}", "thing {thing}");
            result.Should().Be("thing {thing}");
        }
    }
}
