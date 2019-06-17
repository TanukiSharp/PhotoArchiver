using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace PhotoArchiver.Controllers
{
    [Route("api/[controller]")]
    [Produces("text/plain")]
    public class VersionsController : Controller
    {
        public const int ClientVersion = 5;
        public const int ServerVersion = 5;

        private readonly string environmentName;

        public VersionsController(IHostingEnvironment env)
        {
            environmentName = env.EnvironmentName;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok($"{ClientVersion};{ServerVersion};{environmentName}");
        }
    }
}
