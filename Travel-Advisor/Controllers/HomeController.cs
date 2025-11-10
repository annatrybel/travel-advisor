using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Travel_Advisor.Models;

namespace Travel_Advisor.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new PreferencjeViewModel();
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(PreferencjeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var rekomendacja = PobierzRekomendacje(model);

            return View("Results", rekomendacja);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}



