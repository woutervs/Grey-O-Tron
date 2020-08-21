using GreyOTron.Library.Helpers;
using Xunit;
using FluentAssertions;

namespace Grey_O_Tron.Library.Tests
{
    public class TranslationsTests
    {
        [Fact]
        public void Test_TranslationIssues()
        {
            var t = new TranslationHelper(new Languages(new Cache(), null, null));
            var result = t.Translate(0, null, "{0}", "thing {thing}");
            result.Should().Be("thing {thing}");
        }
    }
}
