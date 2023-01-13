namespace DraftPuck.Mvc.App
{
    public class AppController : Controller
    {
        public async Task<IActionResult> Index()
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
}
