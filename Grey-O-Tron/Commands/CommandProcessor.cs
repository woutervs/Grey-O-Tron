using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Features.Metadata;

namespace GreyOTron.Commands
{
    public class CommandProcessor
    {
        private readonly string _prefix;
        private readonly ILifetimeScope _container;

        public CommandProcessor(string prefix, ILifetimeScope container)
        {
            _prefix = prefix;
            _container = container;
        }

        public ICommand Parse(string message)
        {
            message = message.Trim();
            if (!message.StartsWith(_prefix))
            {
                return new NullCommand();
            }

            var commandName = message.Substring(_prefix.Length, message.Length - _prefix.Length);
            var i = commandName.IndexOf(' ');
            if (i >= 0)
            {
                var j = i + 1;
                message = commandName.Substring(j, commandName.Length - j);
                commandName = commandName.Substring(0, i).ToLowerInvariant();
            }
            else
            {
                commandName = commandName.Trim().ToLowerInvariant();
                message = string.Empty;
            }
            var command = _container.Resolve<IEnumerable<Meta<ICommand>>>()
                .FirstOrDefault(a => a.Metadata.ContainsKey("CommandName") && a.Metadata["CommandName"].Equals(commandName))?.Value;
            if (command != null)
            {
                command.Arguments = message;
            }
            else
            {
                command = new NotFoundCommand { Arguments = commandName };
            }
            return command;
        }
    }
}
