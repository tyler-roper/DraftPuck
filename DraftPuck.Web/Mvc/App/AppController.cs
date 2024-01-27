namespace DraftPuck.Web.Mvc.App;

public class AppController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // Disable all other /api/* routes.
    [Route("/api/{**rest}")]
    public IActionResult Api()
    {
        return NotFound("");
    }
}
