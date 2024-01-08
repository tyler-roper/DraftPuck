namespace DraftPuck.Api.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class DraftPuckApiControllerBase : Controller
    {
        public User? CurrentUser => (User?)HttpContext.Items["User"];
    }
}