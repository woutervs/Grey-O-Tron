using GreyOTron.Library.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace GreyOTron.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return VersionResolverHelper.Get();
        }
    }
}