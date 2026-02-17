using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bevera.Models.ViewModels
{
    public class CheckoutVm : IValidatableObject
    {
        public List<CheckoutItemVm> Items { get; set; } = new();

        public decimal Total { get; set; }

        [Required(ErrorMessage = "Въведи име.")]
        [StringLength(120)]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Въведи имейл.")]
        [EmailAddress]
        [StringLength(120)]
        public string? Email { get; set; }

        [StringLength(30)]
        [Required(ErrorMessage = "Въведи телефон.")]
        public string? Phone { get; set; }

        // City is stored inside Order.Address (no DB migration).
        [Required(ErrorMessage = "Избери град.")]
        [StringLength(50)]
        public string? City { get; set; }

        [Required(ErrorMessage = "Въведи адрес.")]
        [StringLength(200)]
        public string? Address { get; set; }

        // "card" or "cash" (simulation)
        [Required]
        public string PaymentMethod { get; set; } = "card";

        // Card (simulation fields)
        [StringLength(60)]
        public string? CardHolder { get; set; }

        [StringLength(19)]
        public string? CardNumber { get; set; }

        [Range(1, 12)]
        public int? ExpMonth { get; set; }

        [Range(2024, 2100)]
        public int? ExpYear { get; set; }

        [StringLength(4)]
        public string? Cvc { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Basic sanity checks
            if (!string.IsNullOrWhiteSpace(Phone))
            {
                var phone = Phone.Trim();
                // BG-friendly: +359XXXXXXXXX or 0XXXXXXXXX
                if (!Regex.IsMatch(phone, @"^(\+359|0)\d{8,9}$"))
                    yield return new ValidationResult("Невалиден телефон. Пример: +359888123456 или 0888123456", new[] { nameof(Phone) });
            }

            if (!string.IsNullOrWhiteSpace(City) && City.Trim().Length < 2)
                yield return new ValidationResult("Градът трябва да е поне 2 символа.", new[] { nameof(City) });

            if (!string.IsNullOrWhiteSpace(Address) && Address.Trim().Length < 5)
                yield return new ValidationResult("Адресът трябва да е поне 5 символа.", new[] { nameof(Address) });

            if (PaymentMethod == "card")
            {
                if (string.IsNullOrWhiteSpace(CardHolder) || CardHolder.Trim().Length < 3)
                    yield return new ValidationResult("Въведи име на картодържател.", new[] { nameof(CardHolder) });

                var digits = (CardNumber ?? "").Replace(" ", "").Replace("-", "");
                // Simulation rule: exactly 10 digits
                if (digits.Length != 10 || digits.Any(ch => !char.IsDigit(ch)))
                    yield return new ValidationResult("Номерът на картата трябва да е точно 10 цифри (симулация).", new[] { nameof(CardNumber) });

                if (ExpMonth == null)
                    yield return new ValidationResult("Избери месец.", new[] { nameof(ExpMonth) });

                if (ExpYear == null)
                    yield return new ValidationResult("Избери година.", new[] { nameof(ExpYear) });

                var cvc = (Cvc ?? "").Trim();
                if (cvc.Length != 3 || cvc.Any(ch => !char.IsDigit(ch)))
                    yield return new ValidationResult("CVC трябва да е точно 3 цифри.", new[] { nameof(Cvc) });
            }
        }
    }

    public class CheckoutItemVm
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total => UnitPrice * Quantity;
    }
}
