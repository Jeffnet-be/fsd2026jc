using Microsoft.AspNetCore.Mvc;

namespace FullStackDevelopment_Ticketverkoop.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
