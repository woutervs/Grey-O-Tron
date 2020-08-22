using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Features.Metadata;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Models;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Library.Helpers
{
    public class CommandResolverHelper
    {
        private readonly ILifetimeScope container;
        private readonly IConfiguration configuration;

        public CommandResolverHelper(ILifetimeScope container, IConfiguration configuration)
        {
            this.container = container;
            this.configuration = configuration;
        }

        private IEnumerable<Command> commands;

        public IEnumerable<Command> Commands
        {
            get { return commands ??= Visualize(); }
        }

        private IEnumerable<Command> Visualize()
        {
            return container.Resolve<IEnumerable<Meta<ICommand>>>().Where(command => command.Metadata.ContainsKey("CommandName")).Select(c => new Command
            {
                Name = configuration["CommandPrefix"] + c.Metadata["CommandName"],
                Description = c.Metadata.ContainsKey("CommandDescription") ? c.Metadata["CommandDescription"]?.ToString() : null,
                Arguments = c.Metadata.ContainsKey("CommandArguments") ? c.Metadata["CommandArguments"]?.ToString() : null,
                Options = c.Metadata.ContainsKey("CommandOptions") ? (CommandOptions)c.Metadata["CommandOptions"] : CommandOptions.None
            }).OrderBy(x => x.Name).ToList();
        }
    }
}
