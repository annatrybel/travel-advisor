using System.ComponentModel.DataAnnotations;

namespace Travel_Advisor.Models
{
    public class PreferencjeViewModel
    {
        [Required]
        [Display(Name = "Maksymalny budżet na osobę (PLN)")]
        [Range(500, 20000)]
        public int BudzetPln { get; set; } = 5000;

        [Required]
        [Display(Name = "Jak długo chcesz podróżować?")]
        public string Czas { get; set; }

        [Required]
        [Display(Name = "Jaki jest główny cel Twojej podróży?")]
        public string StylPodrozy { get; set; }

        [Required]
        [Display(Name = "Jakie otoczenie preferujesz?")]
        public string Otoczenie { get; set; }

        [Required]
        [Display(Name = "Z kim podróżujesz?")]
        public string SkladGrupy { get; set; }
    }
}
