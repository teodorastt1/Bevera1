using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModels.Supply
{
    public class DistributorFormVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(120)]
        [Display(Name = "Име")]
        public string Name { get; set; } = "";

        [StringLength(120)]
        [Display(Name = "Имейл")]
        public string? Email { get; set; }

        [StringLength(30)]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [StringLength(200)]
        [Display(Name = "Адрес")]
        public string? Address { get; set; }

        [StringLength(500)]
        [Display(Name = "Бележки")]
        public string? Notes { get; set; }

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;
    }
}