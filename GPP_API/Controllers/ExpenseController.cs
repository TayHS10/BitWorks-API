using Microsoft.AspNetCore.Mvc;

namespace GPP_API.Controllers
{
    public class ExpenseController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
