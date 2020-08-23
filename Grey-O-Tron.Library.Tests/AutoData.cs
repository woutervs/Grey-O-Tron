using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using AutoFixture.Xunit2;
using Discord;
using Discord.WebSocket;
using FakeItEasy;
using FluentAssertions;

namespace GreyOTron.Library.Tests
{
    public class GreyOTronLibraryInlineAutoDataAttribute : InlineAutoDataAttribute
    {
        public GreyOTronLibraryInlineAutoDataAttribute(params object[] objects) : base(new GreyOTronLibraryAutoDataAttribute(), objects)
        {

        }
    }

    public class GreyOTronLibraryAutoDataAttribute : AutoDataAttribute
    {
        public GreyOTronLibraryAutoDataAttribute() : base(() =>
        {
            var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

            //fixture.Create<IMessage>();

            return fixture;
        })
        {

        }
    }
}
