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
        private static readonly Random _random = new Random();

        public DestinationSeeder(IHttpClientFactory httpClientFactory, IConfiguration configuration, TravelAdvisorContext context)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _context = context;
        }


        private readonly List<SeedPoint> _beachSeedPoints = new List<SeedPoint>
        {
            new SeedPoint { Name = "Riwiera Turecka", Lat = 36.8969, Lon = 30.7133 },
            new SeedPoint { Name = "Wyspy Greckie", Lat = 36.4335, Lon = 25.4323 },
            new SeedPoint { Name = "Wybrzeże Algarve, Portugalia", Lat = 37.0179, Lon = -8.9922 },
            new SeedPoint { Name = "Costa del Sol, Hiszpania", Lat = 36.7213, Lon = -4.4214 }, 
            new SeedPoint { Name = "Phuket, Tajlandia", Lat = 7.8804, Lon = 98.3923 }
        };

        private readonly List<SeedPoint> _citySeedPoints = new List<SeedPoint>
        {
            new SeedPoint { Name = "Praga, Czechy", Lat = 50.0755, Lon = 14.4378 },
            new SeedPoint { Name = "Rzym, Włochy", Lat = 41.9028, Lon = 12.4964 },
            new SeedPoint { Name = "Paryż, Francja", Lat = 48.8566, Lon = 2.3522 },
            new SeedPoint { Name = "Lizbona, Portugalia", Lat = 38.7223, Lon = -9.1393 },
            new SeedPoint { Name = "Tokio, Japonia", Lat = 35.6895, Lon = 139.6917 }
        };

        private readonly List<SeedPoint> _natureSeedPoints = new List<SeedPoint>
        {
            new SeedPoint { Name = "Zakopane, Polska", Lat = 49.2992, Lon = 19.9496 },
            new SeedPoint { Name = "Alpy Szwajcarskie", Lat = 46.8182, Lon = 8.2275 }, 
            new SeedPoint { Name = "Park Narodowy Jezior Plitwickich, Chorwacja", Lat = 44.8653, Lon = 15.5820 },
            new SeedPoint { Name = "Islandia - Złoty Krąg", Lat = 64.2546, Lon = -21.1303 }, 
            new SeedPoint { Name = "Park Narodowy Yosemite, USA", Lat = 37.8651, Lon = -119.5383 }
        };


        public async Task SeedDestinationsAsync()
        {
            if (_context.Destinations.Any())
            {
                Debug.WriteLine("Baza danych zawiera już destinations.");
                return;
            }

            var allFoundDestinations = new List<Destination>();

            foreach (var beachPoint in _beachSeedPoints)
            {
                var beachDestinations = await FindPotentialDestinations("beach.beach_resort,beach", beachPoint.Lat, beachPoint.Lon, 500000);
                allFoundDestinations.AddRange(beachDestinations);
            }

            foreach (var cityPoint in _citySeedPoints)
            {
                var cityDestinations = await FindPotentialDestinations("tourism.sights", cityPoint.Lat, cityPoint.Lon, 800000);
                allFoundDestinations.AddRange(cityDestinations);
            }

            foreach (var naturePoint in _natureSeedPoints)
            {
                var natureDestinations = await FindPotentialDestinations("leisure.park,tourism.attraction.natural", naturePoint.Lat, naturePoint.Lon, 300000);
                allFoundDestinations.AddRange(natureDestinations);
            }

            var natureCategories = new List<string> { "leisure.park", "tourism.attraction.natural" };
            foreach (var naturePoint in _natureSeedPoints)
            {
                foreach (var category in natureCategories)
                {
                    var natureDestinations = await FindPotentialDestinations(category, naturePoint.Lat, naturePoint.Lon, 300000);
                    allFoundDestinations.AddRange(natureDestinations);
                }
            }

            var uniqueDestinations = allFoundDestinations
                .GroupBy(d => d.LocationName)
                .Select(g => g.First())
                .ToList();

            if (uniqueDestinations.Any())
            {
                await _context.Destinations.AddRangeAsync(uniqueDestinations);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<List<Destination>> FindPotentialDestinations(string categories, double lat, double lon, int radius)
        {
            var geoapifyApiKey = _configuration["ApiKeys:Geoapify"];
            var potentialDestinations = new List<Destination>();
            var client = _httpClientFactory.CreateClient();

            Func<string, string> buildUrl = (lang) =>
            {
                var queryParams = new List<string> { $"categories={Uri.EscapeDataString(categories)}", "limit=20", $"apiKey={geoapifyApiKey}", $"lang={lang}" };
                if (categories.Contains("natural") || categories.Contains("park") || categories.Contains("beach"))
                    queryParams.Add($"filter={Uri.EscapeDataString($"circle:{lon.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)},{radius}")}");
                else
                    queryParams.Add($"bias={Uri.EscapeDataString($"proximity:{lon.ToString(CultureInfo.InvariantCulture)},{lat.ToString(CultureInfo.InvariantCulture)}")}");
                return "https://api.geoapify.com/v2/places?" + string.Join("&", queryParams);
            };

            try
            {
                var responsePlTask = client.GetAsync(buildUrl("pl"));
                var responseEnTask = client.GetAsync(buildUrl("en"));
                await Task.WhenAll(responsePlTask, responseEnTask);

                if (!responsePlTask.Result.IsSuccessStatusCode || !responseEnTask.Result.IsSuccessStatusCode) return potentialDestinations;

                var jsonPl = JObject.Parse(await responsePlTask.Result.Content.ReadAsStringAsync());
                var jsonEn = JObject.Parse(await responseEnTask.Result.Content.ReadAsStringAsync());

                var featuresEn = jsonEn["features"]?.ToList() ?? new List<JToken>();
                var enDict = featuresEn.ToDictionary(
                    f => $"{((double)f["properties"]["lon"]):F4},{((double)f["properties"]["lat"]):F4}",
                    f => f["properties"]
                );

                foreach (var feature in jsonPl["features"] ?? new JArray())
                {
                    var propsPl = feature["properties"];
                    string key = $"{((double)propsPl["lon"]):F4},{((double)propsPl["lat"]):F4}";

                    if (enDict.TryGetValue(key, out var propsEn))
                    {
                        var newDest = new Destination
                        {
                            LocationName = propsPl["city"]?.ToString() ?? propsPl["name"]?.ToString(),
                            CountryName = propsPl["country"]?.ToString(),

                            LocationNameEn = propsEn["city"]?.ToString() ?? propsEn["name"]?.ToString(),
                            CountryNameEn = propsEn["country"]?.ToString(),

                            Descriptor = $"Odkryj {propsPl["city"]?.ToString() ?? propsPl["name"]?.ToString()} w kraju {propsPl["country"]?.ToString()}",
                            UnsplashQuery = $"{propsEn["city"]?.ToString() ?? propsEn["name"]?.ToString()}, {propsEn["country"]?.ToString()}"
                        };

                        List<string> travelStyles, environments, durations, groupTypes;
                        if (categories.Contains("beach")) {
                            travelStyles = new List<string> { "odpoczynek" };
                            environments = new List<string> { "plaze" };
                            durations = new List<string> { "tydzien", "dwa_tygodnie" }; 
                            groupTypes = new List<string> { "para", "rodzina", "znajomi" };
                        } else if (categories.Contains("sights")) {
                            travelStyles = new List<string> { "kultura", "rozrywka" }; 
                            environments = new List<string> { "miasto" };
                            durations = new List<string> { "weekend", "tydzien" };
                            groupTypes = new List<string> { "solo", "para", "znajomi" };
                        } else { 
                            travelStyles = new List<string> { "przygoda", "odpoczynek" };
                            environments = new List<string> { "natura" };
                            durations = new List<string> { "weekend", "tydzien", "dwa_tygodnie" }; 
                            groupTypes = new List<string> { "solo", "para", "rodzina", "znajomi" };
                        }
                        newDest.TravelStyles = string.Join(",", travelStyles); 
                        newDest.Environments = string.Join(",", environments);
                        newDest.Durations = string.Join(",", durations);
                        newDest.GroupTypes = string.Join(",", groupTypes);

                        int minBudget, maxBudget;

                        if (categories.Contains("beach"))
                        {
                            minBudget = 3000;
                            maxBudget = 15000;
                        }
                        else if (categories.Contains("sights"))
                        {
                            minBudget = 1000;
                            maxBudget = 8000;
                        }
                        else 
                        {
                            minBudget = 1500;
                            maxBudget = 12000;
                        }

                        int randomBudget = _random.Next(minBudget, maxBudget);
                        newDest.MinBudget = (int)(Math.Round(randomBudget / 100.0) * 100);


                        if (!string.IsNullOrEmpty(newDest.LocationName) && !string.IsNullOrEmpty(newDest.CountryName))
                        {
                            potentialDestinations.Add(newDest);
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Błąd Geoapify: {ex.Message}"); }

            return potentialDestinations.GroupBy(d => d.LocationName).Select(g => g.First()).ToList();
        }
    }
}
