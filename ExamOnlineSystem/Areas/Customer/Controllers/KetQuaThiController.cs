using System.Security.Claims;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Customer.Controllers;

// Sinh viên xem kết quả thi của chính mình (lấy theo tài khoản đang đăng nhập, không nhận id từ URL)
[Area("Customer")]
[Authorize(Roles = SD.Role_Student)]
public class KetQuaThiController : Controller
{
    private readonly ISinhVienService _sinhVienService;

    public KetQuaThiController(ISinhVienService sinhVienService)
    {
        _sinhVienService = sinhVienService;
    }

    // GET: /Customer/KetQuaThi
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sinhVienId = userId == null ? null : await _sinhVienService.GetSinhVienIdByUserIdAsync(userId);
        if (sinhVienId == null)
        {
            // Tài khoản có vai trò Sinh viên nhưng chưa gắn hồ sơ sinh viên
            return View(model: null);
        }

        return View(await _sinhVienService.GetSinhVienDetailAsync(sinhVienId.Value));
    }
}
