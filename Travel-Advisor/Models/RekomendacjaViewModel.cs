namespace Travel_Advisor.Models
{
    public class RekomendacjaViewModel
    {
        public string Tytul { get; set; }
        public string Opis { get; set; }
        public string Uzasadnienie { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Szczegoly { get; set; } = new List<string>();
    }
}