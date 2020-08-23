using System;
using Autofac;
using Autofac.Core;
using Autofac.Extras.AttributeMetadata;
using Autofac.Extras.Moq;
using FluentAssertions;
using GreyOTron.Library.Commands;
using GreyOTron.Library.Helpers;
using Xunit;

namespace GreyOTron.Library.Tests
{
    public class AutoMockFixture : IDisposable
    {
        public AutoMock Mock { get; private set; }
        public void InitAutoMock(Environments environment, Type commandType)
        {
            Environment.SetEnvironmentVariable("Environment", environment.ToString());

            Mock = AutoMock.GetLoose(cfg =>
            {
                cfg.RegisterModule<AttributedMetadataModule>();
                cfg.RegisterType(commandType).AsImplementedInterfaces();
                cfg.RegisterType<CommandProcessor>().AsSelf().WithParameter(
                    new ResolvedParameter((info, context) => info.ParameterType == typeof(string) && info.Name == "prefix",
                        (info, context) => "got#"
                    ));
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
        [InlineData(" got#sync-roles", "", typeof(SyncRolesCommand), Environments.Production)]
        [InlineData(" got#version", "", typeof(VersionCommand), Environments.Production)]
        public void TestCommandProcessor_Expecting_Success(string messageText, string arguments, Type commandType, Environments environment)
        {
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            //act
            var result = commandProcessor.Parse(messageText);
            //assert
            result.Should().BeOfType(commandType);
            result.Arguments.Should().Be(arguments);
        }

        [Theory]
        [InlineData("got#exception", "", typeof(ExceptionCommand), Environments.Maintenance)]
        [InlineData(" got#gw2-key AEREQREW ", "AEREQREW", typeof(Gw2KeyCommand), Environments.Maintenance)]
        [InlineData("got#gw2-remove-key", "", typeof(Gw2RemoveKeyCommand), Environments.Maintenance)]
        [InlineData(" got#gw2-set-main-world 2003  ", "2003", typeof(Gw2SetMainWorldCommand), Environments.Maintenance)]
        [InlineData(" got#gw2-set-worlds 2003;Augury Rock;  ", "2003;Augury Rock;", typeof(Gw2SetWorldsCommand), Environments.Maintenance)]
        [InlineData(" got#gw2-verify", "", typeof(Gw2VerifyCommand), Environments.Maintenance)]
        [InlineData(" got#help ", "", typeof(HelpCommand), Environments.Maintenance)]
        [InlineData("    got#joke ", "", typeof(JokeCommand), Environments.Maintenance)]
        [InlineData("    got#open-breaker ", "", typeof(OpenBreakerCommand), Environments.Maintenance)]
        [InlineData(" got#servers  ", "", typeof(ServersCommand), Environments.Maintenance)]
        [InlineData(" got#set-language nl  ", "nl", typeof(SetLanguageCommand), Environments.Maintenance)]
        [InlineData(" got#set-server-language nl  ", "nl", typeof(SetServerLanguageCommand), Environments.Maintenance)]
        [InlineData(" got#sync-roles", "", typeof(SyncRolesCommand), Environments.Maintenance)]
        [InlineData(" got#version", "", typeof(VersionCommand), Environments.Maintenance)]
        public void TestCommandProcessor_Expecting_Maintenance(string messageText, string arguments, Type commandType, Environments environment)
        {
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            //act
            var result = commandProcessor.Parse(messageText);
            //assert
            result.Should().BeOfType<MaintenanceCommand>();
            result.Arguments.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData("some text", "", typeof(VersionCommand), Environments.Production)]
        [InlineData("some text", "", typeof(VersionCommand), Environments.Maintenance)]
        [InlineData("some text", "", typeof(VersionCommand), Environments.Development)]
        public void TestCommandProcessor_Expecting_Null(string messageText, string arguments, Type commandType, Environments environment)
        {
            //arrange
            autoMockFixture.InitAutoMock(environment, commandType);
            var commandProcessor = autoMockFixture.Mock.Create<CommandProcessor>();
            //act
            var result = commandProcessor.Parse(messageText);
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
            var result = commandProcessor.Parse(messageText);
            //assert
            result.Should().BeOfType<NotFoundCommand>();
            result.Arguments.Should().Be(arguments);
        }


    }
}
