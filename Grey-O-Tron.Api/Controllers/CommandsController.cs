using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Features.Metadata;
using GreyOTron.Library.Commands;
using Microsoft.AspNetCore.Mvc;

namespace GreyOTron.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommandsController : ControllerBase
    {
        private readonly ILifetimeScope container;

        public CommandsController(ILifetimeScope container)
        {
            this.container = container;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            var commands = container.Resolve<IEnumerable<Meta<ICommand>>>().ToList();
            foreach (var command in commands)
            {
                if (command.Metadata.ContainsKey("CommandName"))
                {
                    yield return command.Metadata["CommandName"].ToString();
                }
            }
        }
    }
}