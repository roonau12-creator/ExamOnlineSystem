using System.Security.Claims;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Customer.Controllers;

// Hồ sơ cá nhân của sinh viên đang đăng nhập (không nhận id từ URL)
[Area("Customer")]
[Authorize(Roles = SD.Role_Student)]
public class HoSoController : Controller
{
    private readonly ISinhVienService _sinhVienService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public HoSoController(ISinhVienService sinhVienService, UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _sinhVienService = sinhVienService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Customer/HoSo
    public async Task<IActionResult> Index()
    {
        var vm = await TaoViewModelAsync();
        return vm == null ? View("KhongCoHoSo") : View(vm);
    }

    // POST: /Customer/HoSo/CapNhatLienHe
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatLienHe([Bind(Prefix = nameof(HoSoSinhVienVM.LienHe))] CapNhatLienHeVM lienHe)
    {
        var sinhVienId = await SinhVienIdAsync();
        if (sinhVienId == null)
        {
            return View("KhongCoHoSo");
        }

        if (!ModelState.IsValid)
        {
            var vm = (await TaoViewModelAsync())!;
            vm.LienHe = lienHe;
            return View(nameof(Index), vm);
        }

        await _sinhVienService.CapNhatSoDienThoaiAsync(sinhVienId.Value, lienHe.SoDienThoai);
        TempData["success"] = "Đã cập nhật số điện thoại";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Customer/HoSo/DoiMatKhau
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau([Bind(Prefix = nameof(HoSoSinhVienVM.DoiMatKhau))] DoiMatKhauVM doiMatKhau)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (ModelState.IsValid)
        {
            var result = await _userManager.ChangePasswordAsync(user, doiMatKhau.MatKhauHienTai, doiMatKhau.MatKhauMoi);
            if (result.Succeeded)
            {
                // Đổi mật khẩu làm mới security stamp -> đăng nhập lại để không bị đăng xuất ở phiên hiện tại
                await _signInManager.RefreshSignInAsync(user);
                TempData["success"] = "Đã đổi mật khẩu";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                var truong = error.Code == "PasswordMismatch" ? nameof(DoiMatKhauVM.MatKhauHienTai) : nameof(DoiMatKhauVM.MatKhauMoi);
                ModelState.AddModelError($"{nameof(HoSoSinhVienVM.DoiMatKhau)}.{truong}", DichLoiMatKhau(error));
            }
        }

        var vm = await TaoViewModelAsync();
        if (vm == null)
        {
            return View("KhongCoHoSo");
        }
        return View(nameof(Index), vm); // không trả lại mật khẩu về form
    }

    private async Task<int?> SinhVienIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId == null ? null : await _sinhVienService.GetSinhVienIdByUserIdAsync(userId);
    }

    private async Task<HoSoSinhVienVM?> TaoViewModelAsync()
    {
        var sinhVienId = await SinhVienIdAsync();
        var sinhVien = sinhVienId == null ? null : await _sinhVienService.GetSinhVienByIdAsync(sinhVienId.Value);
        if (sinhVien == null)
        {
            return null;
        }

        var user = await _userManager.GetUserAsync(User);
        return new HoSoSinhVienVM
        {
            SinhVien = sinhVien,
            NgayTaoTaiKhoan = user?.NgayTao,
            LanDangNhapCuoi = user?.LanDangNhapCuoi,
            LienHe = new CapNhatLienHeVM { SoDienThoai = sinhVien.SoDienThoai }
        };
    }

    // Thông báo lỗi mật khẩu của Identity bằng tiếng Việt
    private static string DichLoiMatKhau(IdentityError error) => error.Code switch
    {
        "PasswordMismatch" => "Mật khẩu hiện tại không đúng",
        "PasswordTooShort" => "Mật khẩu mới quá ngắn (ít nhất 6 ký tự)",
        "PasswordRequiresDigit" => "Mật khẩu mới phải có ít nhất một chữ số (0-9)",
        "PasswordRequiresLower" => "Mật khẩu mới phải có ít nhất một chữ thường",
        "PasswordRequiresUpper" => "Mật khẩu mới phải có ít nhất một chữ hoa",
        "PasswordRequiresNonAlphanumeric" => "Mật khẩu mới phải có ít nhất một ký tự đặc biệt",
        "PasswordRequiresUniqueChars" => "Mật khẩu mới cần nhiều ký tự khác nhau hơn",
        _ => error.Description
    };
}
