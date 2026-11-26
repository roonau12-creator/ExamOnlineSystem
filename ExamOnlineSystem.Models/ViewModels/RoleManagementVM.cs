using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Models.ViewModels;

public class RoleManagementVM
{
    public ApplicationUser AplicationUser { get; set; } = null!;
    public IQueryable<SelectListItem> RoleList  { get; set; } = null!;
}