namespace BrewPuck.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class BrewPuckApiControllerBase : Controller {
        public User? CurrentUser => (User?)HttpContext.Items["User"];
    }
}