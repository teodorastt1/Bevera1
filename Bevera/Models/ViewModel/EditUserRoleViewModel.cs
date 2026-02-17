using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bevera.Models.ViewModels
{
    public class EditUserRoleViewModel
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";

        // избраната роля (една роля)
        public string? RoleName { get; set; }

        // всички роли за dropdown
        public List<SelectListItem> Roles { get; set; } = new();
    }
}
