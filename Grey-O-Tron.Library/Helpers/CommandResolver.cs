using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Autofac.Features.Metadata;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Library.Helpers
{
    public class Command
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CommandOptions Options { get; set; }
        public string Arguments { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"> **{Name}** ");
            if (!string.IsNullOrWhiteSpace(Arguments))
            {
                sb.Append($"*{Arguments}*");
            }
            sb.AppendLine();
            sb.AppendLine($"> \t{Description}");
            sb.AppendLine($"> \t{Options}");
            return sb.ToString();
        }
    }
    public class CommandResolver
    {
        private readonly ILifetimeScope container;
        private readonly IConfiguration configuration;

        public CommandResolver(ILifetimeScope container, IConfiguration configuration)
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
                Name = configuration["CommandPrefix"] + c.Metadata["CommandName"]?.ToString(),
                Description = c.Metadata.ContainsKey("CommandDescription") ? c.Metadata["CommandDescription"]?.ToString() : null,
                Arguments = c.Metadata.ContainsKey("CommandArguments") ? c.Metadata["CommandArguments"]?.ToString() : null,
                Options = c.Metadata.ContainsKey("CommandOptions") ? (CommandOptions)c.Metadata["CommandOptions"] : CommandOptions.None
            }).OrderBy(x => x.Name).ToList();
        }
    }
}
