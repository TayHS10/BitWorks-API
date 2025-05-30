using Microsoft.AspNetCore.Mvc;

namespace GPP_API.Controllers
{
    public class AlertController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
