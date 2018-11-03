using Microsoft.AspNetCore.Mvc;

namespace PhotoArchiver.Controllers
{
    [Route("api/[controller]")]
    [Produces("text/plain")]
    public class VersionsController : Controller
    {
        public const int ClientVersion = 3;
        public const int ServerVersion = 4;

        [HttpGet]
        public IActionResult Get()
        {
            return Ok($"{ClientVersion};{ServerVersion}");
        }
    }
}
