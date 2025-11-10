using System.Diagnostics;
using Travel_Advisor.Models;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Travel_Advisor.Models
{
    public class DestinationSeeder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly TravelAdvisorContext _context; 

        public DestinationSeeder(IHttpClientFactory httpClientFactory, IConfiguration configuration, TravelAdvisorContext context)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _context = context;
        }

        public async Task SeedDestinationsAsync()
        {
            if (_context.Destinations.Any())
            {
                Debug.WriteLine("Baza danych zawiera już destinations.");
                return;
            }

            var kurortyTureckie = await FindPotentialDestinations("beach.beach_resort,beach", 36.8969, 30.7133, 500000);
            var miastaEuropejskie = await FindPotentialDestinations("tourism.sights,historic", 50.0755, 14.4378, 800000);
            var miejscaNaturalne = await FindPotentialDestinations("natural,leisure.national_park", 49.2992, 19.9496, 300000);

            var wszystkieMiejsca = kurortyTureckie
                .Concat(miastaEuropejskie)
                .Concat(miejscaNaturalne)
                .GroupBy(d => d.LocationName) 
                .Select(g => g.First())
                .ToList();

            if (wszystkieMiejsca.Any())
            {
                await _context.Destinations.AddRangeAsync(wszystkieMiejsca);
                await _context.SaveChangesAsync();
                Debug.WriteLine($"Zakończono seeding. Dodano {wszystkieMiejsca.Count} nowych destynacji.");
            }
        }

        private async Task<List<Destination>> FindPotentialDestinations(string categories, double lat, double lon, int radius)
        {
            var geoapifyApiKey = _configuration["ApiKeys:Geoapify"];
            var potentialDestinations = new List<Destination>();
            var random = new Random();

            if (string.IsNullOrEmpty(geoapifyApiKey)) return potentialDestinations;

            var client = _httpClientFactory.CreateClient();

            var url = string.Format(CultureInfo.InvariantCulture,
               "https://api.geoapify.com/v2/places?categories={0}&filter=circle:{1},{2},{3}&lang=pl&limit=20&apiKey={4}",
               Uri.EscapeDataString(categories), lon, lat, radius, geoapifyApiKey);

            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);

                    foreach (var feature in json["features"])
                    {
                        var props = feature["properties"];
                        List<string> travelStyles = new List<string>();
                        List<string> environments = new List<string>();

                        switch (categories)
                        {
                            case "tourism.resort,beach":
                                travelStyles = new List<string> { "odpoczynek" };
                                environments = new List<string> { "plaze" };
                                break;
                            case "tourism.city,historic":
                                travelStyles = new List<string> { "kultura", "rozrywka" };
                                environments = new List<string> { "miasto" };
                                break;
                            case "natural_area,national_park":
                                travelStyles = new List<string> { "przygoda", "odpoczynek" };
                                environments = new List<string> { "natura" };
                                break;
                        }

                        var durations = new List<string> { "weekend", "tydzien", "dwa_tygodnie" };
                        var groupTypes = new List<string> { "solo", "para", "rodzina", "znajomi" };

                        var newDest = new Destination
                        {
                            LocationName = props?["name"]?.ToString() ?? props?["city"]?.ToString(),
                            CountryName = props?["country"]?.ToString(),
                            Descriptor = $"Odkryj {props?["name"]?.ToString() ?? "to miejsce"} w kraju {props?["country"]?.ToString()}",
                            UnsplashQuery = $"{props?["name"]?.ToString()}, {props?["country"]?.ToString()} landmark",

                            TravelStyles = string.Join(",", travelStyles),
                            Environments = string.Join(",", environments),
                            Durations = string.Join(",", durations),
                            GroupTypes = string.Join(",", groupTypes)
                        };

                        if (!string.IsNullOrEmpty(newDest.LocationName) && !string.IsNullOrEmpty(newDest.CountryName))
                        {
                            potentialDestinations.Add(newDest);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas generowania destynacji z Geoapify: {ex.Message}");
            }

            return potentialDestinations.GroupBy(d => d.LocationName).Select(g => g.First()).ToList();
        }
    }
}
