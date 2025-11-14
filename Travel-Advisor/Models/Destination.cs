using System.ComponentModel.DataAnnotations;

namespace Travel_Advisor.Models
{
    public class Destination
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public string LocationNameEn { get; set; }
        public string CountryName { get; set; }
        public string CountryNameEn { get; set; }
        public string TravelStyles { get; set; }
        public string Environments { get; set; }
        public string Descriptor { get; set; }
        public string UnsplashQuery { get; set; }
        public string Durations { get; set; }
        public string GroupTypes { get; set; }
    }
}