using Microsoft.AspNetCore.Mvc;

namespace GPP_API.Controllers
{
    public class Notification : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
