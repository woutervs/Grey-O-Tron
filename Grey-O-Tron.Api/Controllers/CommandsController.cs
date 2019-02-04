using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Features.Metadata;
using GreyOTron.Api.Models;
using GreyOTron.Library.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CommandsController : ControllerBase
    {
        private readonly ILifetimeScope container;
        private readonly IConfiguration configuration;

        public CommandsController(ILifetimeScope container, IConfiguration configuration)
        {
            this.container = container;
            this.configuration = configuration;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Command>> Get()
        {
            return container.Resolve<IEnumerable<Meta<ICommand>>>().Where(command => command.Metadata.ContainsKey("CommandName")).Select(c => new Command
            {
                Name = configuration["command-prefix"] + c.Metadata["CommandName"]?.ToString(),
                Description = c.Metadata.ContainsKey("CommandDescription") ? c.Metadata["CommandDescription"]?.ToString() : null
            }).ToList();
        }
    }
}