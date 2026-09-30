using Microsoft.AspNetCore.Mvc;

namespace HotelHup.API.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
