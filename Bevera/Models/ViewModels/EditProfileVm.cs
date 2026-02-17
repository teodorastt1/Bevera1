using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModels
{
    public class EditProfileVm
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = "";

        [Required, StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = "";

        [Required, StringLength(20, MinimumLength = 8)]
        [RegularExpression(@"^(\+359|0)\d{8,9}$", ErrorMessage = "Телефонът трябва да е като 0888123456 или +359888123456")]
        public string PhoneNumber { get; set; } = "";

        [Required, StringLength(120, MinimumLength = 5)]
        public string Address { get; set; } = "";
    }
}
