using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travel_Advisor.Models;

namespace Travel_Advisor.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TravelAdvisorContext _context;

        public HomeController(ILogger<HomeController> logger, TravelAdvisorContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new PreferencjeViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(PreferencjeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var destinations = await _context.Destinations.ToListAsync();

            if (!destinations.Any())
            {
                ModelState.AddModelError("", "Brak dostępnych destynacji w bazie. Proszę uruchomić seeder.");
                return View(model);
            }

            var scoredDestinations = destinations.Select(dest => new
            {
                Destination = dest,
                Score = CalculateMatchScore(dest, model)
            });

            var bestMatch = scoredDestinations
                .Where(s => s.Score > 0) 
                .OrderByDescending(s => s.Score)
                .FirstOrDefault();

            if (bestMatch == null)
            {
                TempData["NoMatchMessage"] = "Niestety, nie znaleźliśmy idealnej propozycji dla podanych kryteriów. Spróbuj zmienić swoje preferencje!";
                return RedirectToAction("Index");
            }

            var matchedDestination = bestMatch.Destination;

            var rekomendacja = new RekomendacjaViewModel
            {
                Tytul = $"{matchedDestination.LocationName}, {matchedDestination.CountryName}",
                Opis = matchedDestination.Descriptor,
                ImageUrl = $"https://source.unsplash.com/1600x900/?{Uri.EscapeDataString(matchedDestination.UnsplashQuery)}",
                Uzasadnienie = $"To miejsce idealnie pasuje do Twojego stylu podróży ('{model.StylPodrozy}') i preferencji otoczenia ('{model.Otoczenie}').",
                Szczegoly = new List<string>
                {
                    $"Długość wyjazdu: Idealna na {model.Czas.Replace("_", " ")}.",
                    $"Idealne dla podróżujących: {model.SkladGrupy}.",
                    $"Budżet: Mieści się w Twoim zakresie.", 
                }
            };

            return View("Results", rekomendacja);
        }

        private int CalculateMatchScore(Destination destination, PreferencjeViewModel preferences)
        {
            int score = 0;


            if (destination.TravelStyles.Contains(preferences.StylPodrozy))
            {
                score += 2; 
            }

            if (destination.Environments.Contains(preferences.Otoczenie))
            {
                score += 2; 
            }

            if (destination.Durations.Contains(preferences.Czas))
            {
                score++;
            }

            if (destination.GroupTypes.Contains(preferences.SkladGrupy))
            {
                score++;
            }

            return score;
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