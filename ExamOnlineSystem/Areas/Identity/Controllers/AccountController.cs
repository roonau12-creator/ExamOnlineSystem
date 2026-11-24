using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Identity.Controllers;

[Area("Identity")]
public class AccountController : Controller
{
    // Một thông báo chung cho sai email / sai mật khẩu, không để lộ email nào đã có tài khoản
    private const string SaiThongTin = "Email hoặc mật khẩu không đúng";

    private static readonly string[] TatCaVaiTro = { SD.Role_Admin, SD.Role_Teacher, SD.Role_Student };

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: /Identity/Account/Login
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Identity/Account/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, SaiThongTin);
            return View(model);
        }

        if (!user.DangHoatDong)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password,
            isPersistent: model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LanDangNhapCuoi = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            TempData["success"] = "Đăng nhập thành công";
            return RedirectToLocal(returnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản tạm thời bị khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau.");
            return View(model);
        }

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản chưa được phép đăng nhập");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, SaiThongTin);
        return View(model);
    }

    // GET: /Identity/Account/Register
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var model = new RegisterVM { RoleList = TaoDanhSachVaiTro() };
        return View(model);
    }

    // POST: /Identity/Account/Register
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        await DamBaoCacVaiTroTonTaiAsync();

        // Chỉ Admin mới được chọn vai trò; người tự đăng ký luôn là Student
        var laAdmin = User.IsInRole(SD.Role_Admin);
        var vaiTro = SD.Role_Student;
        if (laAdmin && !string.IsNullOrEmpty(model.Role))
        {
            if (TatCaVaiTro.Contains(model.Role))
            {
                vaiTro = model.Role;
            }
            else
            {
                ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ");
            }
        }

        if (!ModelState.IsValid)
        {
            model.RoleList = TaoDanhSachVaiTro();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            HoTen = model.FullName,
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, vaiTro);

            // Admin tạo tài khoản hộ người khác thì không đăng nhập vào tài khoản mới
            if (laAdmin)
            {
                TempData["success"] = $"Đã tạo tài khoản {user.Email} với vai trò {vaiTro}";
                return RedirectToAction(nameof(Register));
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["success"] = "Đăng ký thành công";
            return RedirectToLocal(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        model.RoleList = TaoDanhSachVaiTro();
        return View(model);
    }

    // POST: /Identity/Account/Logout
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["success"] = "Đã đăng xuất";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        TempData["error"] = "Bạn không có quyền truy cập trang này";
        return RedirectToAction("Index", "Home", new { area = "Customer" });
    }

    // Chỉ chuyển hướng tới URL trong hệ thống, tránh open redirect
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home", new { area = "Customer" });
    }

    // Danh sách vai trò chỉ hiển thị cho Admin, người dùng thường nhận danh sách rỗng
    private IEnumerable<SelectListItem> TaoDanhSachVaiTro()
    {
        if (!User.IsInRole(SD.Role_Admin))
        {
            return Enumerable.Empty<SelectListItem>();
        }

        return TatCaVaiTro.Select(r => new SelectListItem { Text = r, Value = r }).ToList();
    }

    // Tạo từng vai trò nếu còn thiếu (trước đây chỉ kiểm tra Student)
    private async Task DamBaoCacVaiTroTonTaiAsync()
    {
        foreach (var role in TatCaVaiTro)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
