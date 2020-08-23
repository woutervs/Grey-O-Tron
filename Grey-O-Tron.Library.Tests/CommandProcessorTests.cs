using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Extras.AttributeMetadata;
using Autofac.Extras.Moq;
using Autofac.Features.Metadata;
using Discord;
using FakeItEasy;
using FluentAssertions;
using GreyOTron.Library.Commands;
using GreyOTron.Library.Commands.GW2Commands;
using GreyOTron.Library.Commands.ManualCommands;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using Moq;
using Xunit;

namespace GreyOTron.Library.Tests
{
    public class AutoMockFixture : IDisposable
    {
        public AutoMock Mock { get; private set; }
        public void InitAutoMock(Environments environment, Type commandType)
        {
            var environmentHelper = new Mock<IEnvironmentHelper>();
            environmentHelper.Setup(x => x.Current).Returns(environment);
            environmentHelper.Setup(x => x.Is(It.IsAny<Environments>())).Returns<Environments>(x => x == environment);

            Mock = AutoMock.GetLoose(cfg =>
            {
                cfg.RegisterModule<AttributedMetadataModule>();
                cfg.RegisterType(commandType).AsImplementedInterfaces();
                cfg.RegisterType<CommandProcessor>().AsSelf().WithParameter(
                    new ResolvedParameter((info, context) => info.ParameterType == typeof(string) && info.Name == "prefix",
                        (info, context) => "got#"
                    ));
                cfg.RegisterMock(environmentHelper).AsImplementedInterfaces().SingleInstance();
            });
        }
        public void Dispose()
        {
            Mock?.Dispose();
        }
    }

    public class CommandProcessorTests : IClassFixture<AutoMockFixture>
    {
        private readonly AutoMockFixture autoMockFixture;

        public CommandProcessorTests(AutoMockFixture autoMockFixture)
        {
            this.autoMockFixture = autoMockFixture;
        }

        [Theory]
        [InlineData("got#exception", "", typeof(ExceptionCommand), Environments.Production)]
        [InlineData(" got#gw2-key AEREQREW ", "AEREQREW", typeof(Gw2KeyCommand), Environments.Production)]
        [InlineData("got#gw2-remove-key", "", typeof(Gw2RemoveKeyCommand), Environments.Production)]
        [InlineData(" got#gw2-set-main-world 2003  ", "2003", typeof(Gw2SetMainWorldCommand), Environments.Production)]
        [InlineData(" got#gw2-set-worlds 2003;Augury Rock;  ", "2003;Augury Rock;", typeof(Gw2SetWorldsCommand), Environments.Production)]
        [InlineData(" got#gw2-verify", "", typeof(Gw2VerifyCommand), Environments.Production)]
        [InlineData(" got#help ", "", typeof(HelpCommand), Environments.Production)]
        [InlineData("    got#joke ", "", typeof(JokeCommand), Environments.Production)]
        [InlineData("    got#open-breaker ", "", typeof(OpenBreakerCommand), Environments.Production)]
        [InlineData(" got#servers  ", "", typeof(ServersCommand), Environments.Production)]
        [InlineData(" got#set-language nl  ", "nl", typeof(SetLanguageCommand), Environments.Production)]
        [InlineData(" got#set-server-language nl  ", "nl", typeof(SetServerLanguageCommand), Environments.Production)]
        [InlineData(" got#gw2-sync-roles", "", typeof(Gw2SyncRolesCommand), Environments.Production)]
        [InlineData(" got#version", "", typeof(VersionCommand), Environments.Production)]
        public void TestCommandProcessor_Expecting_Success_In_Production(string messageText, string arguments, Type commandType, Environments environment)
        {
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            //act
            var result = commandProcessor.Parse(messageText).Value;
            //assert
            result.Should().BeOfType(commandType);
            result.Arguments.Should().Be(arguments);
        }

        [Theory]
        [InlineData("got#exception", typeof(ExceptionCommand), Environments.Maintenance)]
        [InlineData(" got#gw2-key AEREQREW ", typeof(Gw2KeyCommand), Environments.Maintenance)]
        [InlineData("got#gw2-remove-key", typeof(Gw2RemoveKeyCommand), Environments.Maintenance)]
        [InlineData(" got#gw2-set-main-world 2003  ", typeof(Gw2SetMainWorldCommand), Environments.Maintenance)]
        [InlineData(" got#gw2-set-worlds 2003;Augury Rock;  ", typeof(Gw2SetWorldsCommand), Environments.Maintenance)]
        [InlineData(" got#gw2-verify", typeof(Gw2VerifyCommand), Environments.Maintenance)]
        [InlineData(" got#help ", typeof(HelpCommand), Environments.Maintenance)]
        [InlineData("    got#joke ", typeof(JokeCommand), Environments.Maintenance)]
        [InlineData("    got#open-breaker ", typeof(OpenBreakerCommand), Environments.Maintenance)]
        [InlineData(" got#servers  ", typeof(ServersCommand), Environments.Maintenance)]
        [InlineData(" got#set-language nl  ", typeof(SetLanguageCommand), Environments.Maintenance)]
        [InlineData(" got#set-server-language nl  ", typeof(SetServerLanguageCommand), Environments.Maintenance)]
        [InlineData(" got#gw2-sync-roles", typeof(Gw2SyncRolesCommand), Environments.Maintenance)]
        [InlineData(" got#version", typeof(VersionCommand), Environments.Maintenance)]
        public void TestCommandProcessor_Expecting_Maintenance(string messageText, Type commandType, Environments environment)
        {
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            //act
            var result = commandProcessor.Parse(messageText).Value;
            //assert
            result.Should().BeOfType<MaintenanceCommand>();
            result.Arguments.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData("some text", typeof(VersionCommand), Environments.Production)]
        [InlineData("some text", typeof(VersionCommand), Environments.Maintenance)]
        [InlineData("some text", typeof(VersionCommand), Environments.Development)]
        public void TestCommandProcessor_Expecting_Null(string messageText, Type commandType, Environments environment)
        {
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            //act
            var result = commandProcessor.Parse(messageText).Value;
            //assert
            result.Should().BeOfType<NullCommand>();
            result.Arguments.Should().BeNull();
        }

        [Theory]
        [InlineData("got#command-that-doesn't-exist", "command-that-doesn't-exist", typeof(VersionCommand), Environments.Production)]
        [InlineData("got#command-that-doesn't-exist", "command-that-doesn't-exist", typeof(VersionCommand), Environments.Development)]
        public void TestCommandProcessor_Expecting_NotFound(string messageText, string arguments, Type commandType, Environments environment)
        {
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            //act
            var result = commandProcessor.Parse(messageText).Value;
            //assert
            result.Should().BeOfType<NotFoundCommand>();
            result.Arguments.Should().Be(arguments);
        }

        [Theory]
        [GreyOTronLibraryInlineAutoData("got#exception", typeof(ExceptionCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-key AEREQREW ", typeof(Gw2KeyCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData("got#gw2-remove-key", typeof(Gw2RemoveKeyCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-set-main-world 2003  ", typeof(Gw2SetMainWorldCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-set-worlds 2003;Augury Rock;  ", typeof(Gw2SetWorldsCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-verify", typeof(Gw2VerifyCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#help ", typeof(HelpCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData("    got#joke ", typeof(JokeCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData("    got#open-breaker ", typeof(OpenBreakerCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#servers  ", typeof(ServersCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#set-language nl  ", typeof(SetLanguageCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#set-server-language nl  ", typeof(SetServerLanguageCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-sync-roles", typeof(Gw2SyncRolesCommand), Environments.Production)]
        [GreyOTronLibraryInlineAutoData(" got#version", typeof(VersionCommand), Environments.Production)]
        public async Task TestCommandProcessor_ExecuteMetaCommand_AsOwner(string messageText, Type commandType, Environments environment, IMessage message, IGuildUser guildUser)
        {
            Extensions.UserExtensions.OwnerId = guildUser.Id;
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            A.CallTo(() => message.Author).Returns(guildUser);
            var permissions = new GuildPermissions(administrator: false);
            A.CallTo(() => guildUser.GuildPermissions).Returns(permissions);
            var command = new Fake<ICommand>();
            //act
            var result = commandProcessor.Parse(messageText);
            var toExecute = new Meta<ICommand>(command.FakedObject, result.Metadata);
            await toExecute.Execute(null, message, CancellationToken.None);
            //assert
            A.CallTo(() => toExecute.Value.Execute(message, CancellationToken.None)).MustHaveHappened();

        }

        [Theory]
        [GreyOTronLibraryInlineAutoData("got#exception", typeof(ExceptionCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-key AEREQREW ", typeof(Gw2KeyCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData("got#gw2-remove-key", typeof(Gw2RemoveKeyCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-set-main-world 2003  ", typeof(Gw2SetMainWorldCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-set-worlds 2003;Augury Rock;  ", typeof(Gw2SetWorldsCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-verify", typeof(Gw2VerifyCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#help ", typeof(HelpCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData("    got#joke ", typeof(JokeCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData("    got#open-breaker ", typeof(OpenBreakerCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#servers  ", typeof(ServersCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#set-language nl  ", typeof(SetLanguageCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#set-server-language nl  ", typeof(SetServerLanguageCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-sync-roles", typeof(Gw2SyncRolesCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#version", typeof(VersionCommand), Environments.Production, true)]
        public async Task TestCommandProcessor_ExecuteMetaCommand_AsAdministrator(string messageText, Type commandType, Environments environment, bool mustHaveHappened, IMessage message, IGuildUser guildUser)
        {
            Extensions.UserExtensions.OwnerId = 1;
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            A.CallTo(() => message.Author).Returns(guildUser);
            var permissions = new GuildPermissions(administrator: true);
            A.CallTo(() => guildUser.GuildPermissions).Returns(permissions);
            var command = new Fake<ICommand>();
            //act
            var result = commandProcessor.Parse(messageText);
            var toExecute = new Meta<ICommand>(command.FakedObject, result.Metadata);
            await toExecute.Execute(null, message, CancellationToken.None);
            //assert
            if (mustHaveHappened)
            {
                A.CallTo(() => toExecute.Value.Execute(message, CancellationToken.None)).MustHaveHappened();
            }
            else
            {
                A.CallTo(() => toExecute.Value.Execute(message, CancellationToken.None)).MustNotHaveHappened();
            }

        }

        [Theory]
        [GreyOTronLibraryInlineAutoData("got#exception", typeof(ExceptionCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-key AEREQREW ", typeof(Gw2KeyCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData("got#gw2-remove-key", typeof(Gw2RemoveKeyCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-set-main-world 2003  ", typeof(Gw2SetMainWorldCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-set-worlds 2003;Augury Rock;  ", typeof(Gw2SetWorldsCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-verify", typeof(Gw2VerifyCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#help ", typeof(HelpCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData("    got#joke ", typeof(JokeCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData("    got#open-breaker ", typeof(OpenBreakerCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#servers  ", typeof(ServersCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#set-language nl  ", typeof(SetLanguageCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#set-server-language nl  ", typeof(SetServerLanguageCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-sync-roles", typeof(Gw2SyncRolesCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#version", typeof(VersionCommand), Environments.Production, true)]
        public async Task TestCommandProcessor_ExecuteMetaCommand_AsUser(string messageText, Type commandType, Environments environment, bool mustHaveHappened, IMessage message, IGuildUser guildUser)
        {
            Extensions.UserExtensions.OwnerId = 1;
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            A.CallTo(() => message.Author).Returns(guildUser);
            var permissions = new GuildPermissions(administrator: false);
            A.CallTo(() => guildUser.GuildPermissions).Returns(permissions);
            var command = new Fake<ICommand>();
            //act
            var result = commandProcessor.Parse(messageText);
            var toExecute = new Meta<ICommand>(command.FakedObject, result.Metadata);
            await toExecute.Execute(null, message, CancellationToken.None);
            //assert
            if (mustHaveHappened)
            {
                A.CallTo(() => toExecute.Value.Execute(message, CancellationToken.None)).MustHaveHappened();
            }
            else
            {
                A.CallTo(() => toExecute.Value.Execute(message, CancellationToken.None)).MustNotHaveHappened();
            }

        }

        [Theory]
        [GreyOTronLibraryInlineAutoData("got#exception", typeof(ExceptionCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-key AEREQREW ", typeof(Gw2KeyCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData("got#gw2-remove-key", typeof(Gw2RemoveKeyCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-set-main-world 2003  ", typeof(Gw2SetMainWorldCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-set-worlds 2003;Augury Rock;  ", typeof(Gw2SetWorldsCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-verify", typeof(Gw2VerifyCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#help ", typeof(HelpCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData("    got#joke ", typeof(JokeCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData("    got#open-breaker ", typeof(OpenBreakerCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#servers  ", typeof(ServersCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#set-language nl  ", typeof(SetLanguageCommand), Environments.Production, true)]
        [GreyOTronLibraryInlineAutoData(" got#set-server-language nl  ", typeof(SetServerLanguageCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#gw2-sync-roles", typeof(Gw2SyncRolesCommand), Environments.Production, false)]
        [GreyOTronLibraryInlineAutoData(" got#version", typeof(VersionCommand), Environments.Production, true)]
        public async Task TestCommandProcessor_ExecuteMetaCommand_AsOwner_InDm(string messageText, Type commandType, Environments environment, bool mustHaveHappened, IMessage message, IUser user)
        {
            Extensions.UserExtensions.OwnerId = user.Id;
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            A.CallTo(() => message.Author).Returns(user);
            var command = new Fake<ICommand>();
            //act
            var result = commandProcessor.Parse(messageText);
            var toExecute = new Meta<ICommand>(command.FakedObject, result.Metadata);
            await toExecute.Execute(null, message, CancellationToken.None);
            //assert
            if (mustHaveHappened)
            {
                A.CallTo(() => toExecute.Value.Execute(message, CancellationToken.None)).MustHaveHappened();
            }
            else
            {
                A.CallTo(() => toExecute.Value.Execute(message, CancellationToken.None)).MustNotHaveHappened();
            }

        }

    }
}
