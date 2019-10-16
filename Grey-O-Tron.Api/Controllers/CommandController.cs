using System.Collections.Generic;
using System.Linq;
using GreyOTron.Library.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GreyOTron.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CommandController : ControllerBase
    {
        private readonly CommandResolver resolver;

        public CommandController(CommandResolver resolver)
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