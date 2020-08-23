using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Features.Metadata;
using Discord.Commands;
using GreyOTron.Library.Commands.ManualCommands;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Helpers
{
    public class CommandProcessor
    {
        private readonly string prefix;
        private readonly ILifetimeScope container;
        private readonly IEnvironmentHelper environmentHelper;

        public CommandProcessor(string prefix, ILifetimeScope container, IEnvironmentHelper environmentHelper)
        {
            this.prefix = prefix;
            this.container = container;
            this.environmentHelper = environmentHelper;
        }

        public Meta<ICommand> Parse(string message)
        {
            message = message.Trim();
            if (!message.StartsWith(prefix))
            {
                return new Meta<ICommand>(new NullCommand(), new Dictionary<string, object?>());
            }

            if (environmentHelper.Is(Environments.Maintenance))
            {
                return new Meta<ICommand>(new MaintenanceCommand(), new Dictionary<string, object?>());
            }
            
            var commandName = message[prefix.Length..];
            var i = commandName.IndexOf(' ');
            if (i >= 0)
            {
                message = commandName[(i+1)..];
                commandName = commandName.Substring(0, i).ToLowerInvariant();
            }
            else
            {
                commandName = commandName.Trim().ToLowerInvariant();
                message = string.Empty;
            }
            var command = container.Resolve<IEnumerable<Meta<ICommand>>>()
                .FirstOrDefault(a => a.Metadata.ContainsKey(nameof(Attributes.CommandAttribute.CommandName)) && a.Metadata[nameof(Attributes.CommandAttribute.CommandName)].Equals(commandName));
            if (command != null)
            {
                command.Value.Arguments = message;
            }
            else
            {
                command = new Meta<ICommand>(new NotFoundCommand { Arguments = commandName }, new Dictionary<string, object?>());
            }
            return command;
        }
    }
}
