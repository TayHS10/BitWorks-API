using Microsoft.AspNetCore.Mvc;

namespace GPP_API.Controllers
{
    public class BudgetPartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
