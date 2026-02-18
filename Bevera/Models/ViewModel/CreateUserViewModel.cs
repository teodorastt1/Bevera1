using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bevera.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required, EmailAddress]
        [StringLength(120)]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Паролата трябва да е поне 6 символа.")]
        public string Password { get; set; } = "";

        [StringLength(60)]
        public string? FirstName { get; set; }

        [StringLength(60)]
        public string? LastName { get; set; }

        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public string? RoleName { get; set; }

        public List<SelectListItem> Roles { get; set; } = new();
    }
}
