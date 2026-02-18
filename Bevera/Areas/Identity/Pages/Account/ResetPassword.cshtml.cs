using System.ComponentModel.DataAnnotations;
using Bevera.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bevera.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ResetPasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "Паролата трябва да е поне {2} символа.", MinimumLength = 6)]
            public string Password { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(Password), ErrorMessage = "Паролите не съвпадат.")]
            public string ConfirmPassword { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync(string? email = null)
        {
            // ако е логнат -> auto-fill email
            if (User?.Identity?.IsAuthenticated == true)
            {
                var u = await _userManager.GetUserAsync(User);
                Input.Email = u?.Email ?? "";
                return Page();
            }

            // ако идва от Login с ?email=
            if (!string.IsNullOrWhiteSpace(email))
                Input.Email = email.Trim();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Няма потребител с този имейл.");
                return Page();
            }

            // DEMO reset без token: Remove + Add password
            if (await _userManager.HasPasswordAsync(user))
            {
                var removeRes = await _userManager.RemovePasswordAsync(user);
                if (!removeRes.Succeeded)
                {
                    foreach (var e in removeRes.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    return Page();
                }
            }

            var addRes = await _userManager.AddPasswordAsync(user, Input.Password);
            if (!addRes.Succeeded)
            {
                foreach (var e in addRes.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            if (User?.Identity?.IsAuthenticated == true)
                await _signInManager.RefreshSignInAsync(user);

            TempData["FlashMessage"] = "Успешно сменихте паролата си.";
            TempData["FlashType"] = "success";

            return RedirectToPage("./ResetPasswordConfirmation");
        }
    }
}
