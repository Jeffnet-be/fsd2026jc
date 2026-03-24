using Microsoft.AspNetCore.Mvc;

namespace FullStackDevelopment_Ticketverkoop.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
