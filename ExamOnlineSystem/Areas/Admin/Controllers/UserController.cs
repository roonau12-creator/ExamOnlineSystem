using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

// Quản lý tài khoản đăng nhập: danh sách, thêm, sửa, khóa/mở khóa. Chỉ Admin được vào.
[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IKhoaService _khoaService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(IUserService userService, IKhoaService khoaService,
        UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _khoaService = khoaService;
        _userManager = userManager;
    }

    // GET: /Admin/User – bảng được user.js tải qua GetAllUser
    public async Task<IActionResult> Index()
    {
        // Danh sách khoa cho ô lọc "Khoa"
        ViewData["KhoaList"] = (await _khoaService.GetAllKhoaByAsync()).Select(k => k.TenKhoa).ToList();
        return View();
    }

    // GET: /Admin/User/Create
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new UserCreateVM { VaiTro = SD.Role_Teacher });
    }

    // POST: /Admin/User/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateVM vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.Email) && await _userService.EmailExistsAsync(vm.Email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng cho tài khoản khác");
        }

        if (ModelState.IsValid)
        {
            var (success, errors) = await _userService.CreateUserAsync(vm);
            if (success)
            {
                TempData["success"] = $"Đã tạo tài khoản {vm.Email.Trim()} với vai trò {VaiTroLabel.Ten(vm.VaiTro)}";
                return RedirectToAction(nameof(Index));
            }

            // Lỗi từ Identity (vd: mật khẩu chưa đủ mạnh) hoặc khoa không tồn tại
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        vm.MatKhau = string.Empty; // không trả lại mật khẩu về form
        await LoadDropdowns(vm.KhoaId);
        return View(vm);
    }

    // GET: /Admin/User/Edit/{id}
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var vm = await _userService.GetUserForEditAsync(id);
        if (vm == null)
        {
            return NotFound();
        }

        await LoadDropdowns(vm.KhoaId);
        return View(vm);
    }

    // POST: /Admin/User/Edit/{id} – [FromRoute]: id lấy từ URL, không bị ô "Id" trong form ghi đè
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] string id, UserEditVM vm)
    {
        var hienTai = string.IsNullOrWhiteSpace(id) ? null : await _userService.GetUserForEditAsync(id);
        if (hienTai == null || id != vm.Id)
        {
            return NotFound();
        }

        // Các trường chỉ để hiển thị, lấy lại từ DB (không tin dữ liệu form)
        vm.DangHoatDong = hienTai.DangHoatDong;
        vm.MaSinhVien = hienTai.MaSinhVien;
        vm.MaGiaoVien = hienTai.MaGiaoVien;

        if (!string.IsNullOrWhiteSpace(vm.Email) && await _userService.EmailExistsAsync(vm.Email, id))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng cho tài khoản khác");
        }

        if (ModelState.IsValid)
        {
            var (success, errors) = await _userService.UpdateUserAsync(vm, _userManager.GetUserId(User) ?? "");
            if (success)
            {
                TempData["success"] = $"Đã cập nhật tài khoản {vm.Email.Trim()}";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        vm.MatKhauMoi = null;
        await LoadDropdowns(vm.KhoaId);
        return View(vm);
    }

    // Dropdown Khoa (0 = không gắn khoa) và Vai trò
    private async Task LoadDropdowns(int? khoaDangChon = null)
    {
        var khoas = await _khoaService.GetAllKhoaByAsync();
        ViewData["KhoaSelect"] = khoas
            .OrderBy(k => k.SoThuTu)
            .Select(k => new SelectListItem
            {
                Value = k.Id.ToString(),
                Text = $"{k.MaKhoa} – {k.TenKhoa}",
                Selected = k.Id == khoaDangChon
            })
            .ToList();

        ViewData["VaiTroSelect"] = VaiTroLabel.TatCa
            .Select(r => new SelectListItem { Value = r, Text = VaiTroLabel.Ten(r) })
            .ToList();
    }

    #region CALL API

    public async Task<IActionResult> GetAllUser()
    {
        var users = await _userService.GetAllUserAsync();
        var currentUserId = _userManager.GetUserId(User);

        var data = users.Select(u => new
        {
            u.Id,
            u.HoTen,
            u.Email,
            u.SoDienThoai,
            ChucDanh = u.ChucDanh ?? "",
            u.VaiTro,
            TenVaiTro = string.IsNullOrEmpty(u.VaiTro)
                ? VaiTroLabel.Ten(null)
                : string.Join(", ", u.VaiTro.Split(", ").Select(VaiTroLabel.Ten)),
            u.TenKhoa,
            u.DangHoatDong,
            TrangThai = u.DangHoatDong ? "Đang hoạt động" : "Đã khóa",
            // Hồ sơ nghiệp vụ đang gắn với tài khoản, vd: "SV 2251234567"
            HoSo = u.MaSinhVien != null ? $"SV {u.MaSinhVien}" : u.MaGiaoVien != null ? $"GV {u.MaGiaoVien}" : "",
            NgayTao = u.NgayTao.ToLocalTime().ToString("dd/MM/yyyy"),
            NgayTaoSort = u.NgayTao.ToString("yyyyMMddHHmmss"),
            LanDangNhapCuoi = u.LanDangNhapCuoi?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "",
            LanDangNhapCuoiSort = u.LanDangNhapCuoi?.ToString("yyyyMMddHHmmss") ?? "",
            // Tài khoản đang đăng nhập: không cho tự khóa / tự đổi vai trò
            LaChinhMinh = u.Id == currentUserId
        });

        return Json(new { data });
    }

    // POST: /Admin/User/DoiTrangThai/{id}?khoa=true – khóa / mở khóa tài khoản
    [HttpPost]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ user.js
    public async Task<IActionResult> DoiTrangThai(string id, bool khoa)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Json(new { success = false, message = "Không tìm thấy tài khoản" });
        }

        var (success, message) = await _userService.SetTrangThaiAsync(id, khoa, _userManager.GetUserId(User) ?? "");
        return Json(new { success, message });
    }

    #endregion
}
