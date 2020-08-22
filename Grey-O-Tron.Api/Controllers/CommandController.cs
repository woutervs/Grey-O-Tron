using System.Collections.Generic;
using System.Linq;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Models;
using Microsoft.AspNetCore.Mvc;

namespace GreyOTron.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CommandController : ControllerBase
    {
        private readonly CommandResolverHelper resolver;

        public CommandController(CommandResolverHelper resolver)
        {
            this.resolver = resolver;
        }

        [HttpGet]
        public IEnumerable<Command> Get()
        {
            return resolver.Commands.Where(x => !x.Options.HasFlag(CommandOptions.RequiresOwner));
        }
    }
}