using Microsoft.AspNetCore.Mvc;

namespace GPP_API.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
