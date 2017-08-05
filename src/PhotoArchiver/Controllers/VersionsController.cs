using Microsoft.AspNetCore.Mvc;

namespace PhotoArchiver.Controllers
{
    [Route("api/[controller]")]
    [Produces("text/plain")]
    public class VersionsController : Controller
    {
        public const int ClientVersion = 2;
        public const int ServerVersion = 3;

        [HttpGet]
        public IActionResult Get()
        {
            return Ok($"{ClientVersion};{ServerVersion}");
        }
    }
}
