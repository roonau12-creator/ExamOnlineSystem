using System.Security.Claims;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly ISinhVienService _sinhVienService;

    public HomeController(ISinhVienService sinhVienService)
    {
        _sinhVienService = sinhVienService;
    }

    // Trang chủ: sinh viên thấy dashboard của mình; người khác thấy trang giới thiệu
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole(SD.Role_Student))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sinhVienId = userId == null ? null : await _sinhVienService.GetSinhVienIdByUserIdAsync(userId);
            var vm = sinhVienId == null ? null : await _sinhVienService.GetSinhVienDetailAsync(sinhVienId.Value);
            return View("DashboardSinhVien", vm);
        }

        return View();
    }
}
