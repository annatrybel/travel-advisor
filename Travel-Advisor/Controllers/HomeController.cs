using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;
using Travel_Advisor.Models;

namespace Travel_Advisor.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TravelAdvisorContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, TravelAdvisorContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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

            var bestMatches = scoredDestinations
                .Where(s => s.Score > 0) 
                .OrderByDescending(s => s.Score)
                .Take(3)
                .ToList();

            if (!bestMatches.Any())
            {
                TempData["NoMatchMessage"] = "Niestety, nie znaleźliśmy idealnej propozycji dla podanych kryteriów. Spróbuj zmienić swoje preferencje!";
                return RedirectToAction("Index");
            }

            var rekomendacje = new List<RekomendacjaViewModel>();

            foreach (var match in bestMatches)
            {
                var destination = match.Destination;

                string imageUrl = await GetUnsplashImageUrlAsync(destination, model);

                var rekomendacja = new RekomendacjaViewModel
                {
                    Tytul = $"{destination.LocationName}, {destination.CountryName}",
                    Opis = destination.Descriptor,
                    ImageUrl = imageUrl, 
                    Uzasadnienie = $"To miejsce dobrze pasuje do Twojego stylu podróży ('{model.StylPodrozy}') i preferencji otoczenia ('{model.Otoczenie}').",
                    Szczegoly = new List<string>
                    {
                        $"Długość wyjazdu: Idealna na {model.Czas.Replace("_", " ")}.",
                        $"Idealne dla podróżujących: {model.SkladGrupy}.",
                        "Budżet: Mieści się w Twoim zakresie.",
                    }
                };
                rekomendacje.Add(rekomendacja);
            }

            return View("Results", rekomendacje);
        }

        private async Task<string> GetUnsplashImageUrlAsync(Destination destination, PreferencjeViewModel preferences)
        {
            string unsplashApiKey = _configuration["ApiKeys:Unsplash"];
            string fallbackImageUrl = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688";

            if (string.IsNullOrEmpty(unsplashApiKey))
            {
                _logger.LogError("Klucz API Unsplash nie jest skonfigurowany w appsettings.json");
                return fallbackImageUrl;
            }

            var query = $"{destination.LocationNameEn}, {destination.CountryNameEn}";

            switch (preferences.StylPodrozy)
            {
                case "odpoczynek": query += ", relax, peaceful, calm"; break;
                case "kultura": query += ", history, architecture, museum"; break;
                case "przygoda": query += ", adventure, hiking, wild, action"; break;
                case "rozrywka": query += ", entertainment, city life, fun"; break;
            }
            switch (preferences.Otoczenie)
            {
                case "plaze": query += ", beach, sea, coast, sunny"; break;
                case "miasto": query += ", cityscape, urban"; break;
                case "natura": query += ", nature, landscape, mountains, forest"; break;
                case "egzotyka": query += ", exotic, tropical, jungle"; break;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", unsplashApiKey);

                var requestUrl = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(query)}&per_page=1&orientation=landscape";

                _logger.LogInformation($"[UNSPLASH] Zapytanie API: {requestUrl}");

                var response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);

                    var firstResult = json["results"]?.FirstOrDefault();
                    if (firstResult != null)
                    {
                        return firstResult["urls"]?["regular"]?.ToString() ?? fallbackImageUrl;
                    }
                }
                else
                {
                    _logger.LogError($"Błąd zapytania do API Unsplash: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wyjątek podczas pobierania obrazka z Unsplash.");
            }

            return fallbackImageUrl; 
        }


        private int CalculateMatchScore(Destination destination, PreferencjeViewModel preferences)
        {
            int score = 0;
            if (destination.TravelStyles.Contains(preferences.StylPodrozy)) score += 2;
            if (destination.Environments.Contains(preferences.Otoczenie)) score += 2;
            if (destination.Durations.Contains(preferences.Czas)) score++;
            if (destination.GroupTypes.Contains(preferences.SkladGrupy)) score++;
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