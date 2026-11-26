using System.Security.Claims;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class GiaoVienController : Controller
{
    private readonly IGiaoVienService _giaoVienService;
    private readonly IBoMonService _boMonService;
    private readonly IKhoaService _khoaService;

    public GiaoVienController(IGiaoVienService giaoVienService, IBoMonService boMonService, IKhoaService khoaService)
    {
        _giaoVienService = giaoVienService;
        _boMonService = boMonService;
        _khoaService = khoaService;
    }

    // GET: /Admin/GiaoVien?khoaId=6&boMonId=2 (chọn sẵn bộ lọc khi mở từ trang chi tiết khoa / bộ môn)
    [Authorize(Roles =SD.Role_Admin)]
    public async Task<IActionResult> Index(int? khoaId, int? boMonId)
    {
        // Bộ lọc lấy đủ khoa / bộ môn (kể cả chưa có giảng viên), sắp theo số thứ tự
        var boMons = (await _boMonService.GetAllBoMonAsync(includeKhoa: true)).ToList();
        if (boMonId != null && khoaId == null)
        {
            khoaId = boMons.FirstOrDefault(b => b.Id == boMonId)?.KhoaId;
        }

        ViewData["KhoaList"] = new SelectList(await _khoaService.GetAllKhoaByAsync(), "Id", "TenKhoa", khoaId);
        ViewData["BoMonFilter"] = boMons;
        ViewData["BoMonId"] = boMonId;
        return View();
    }

    // GET: /Admin/GiaoVien/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vm = await _giaoVienService.GetGiaoVienDetailAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    // GET: /Admin/GiaoVien/Create?boMonId=3 (boMonId: chọn sẵn bộ môn khi mở từ trang chi tiết bộ môn)
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> Create(int? boMonId)
    {
        await LoadBoMonList(boMonId);
        return View(new GiaoVienCreateVM { BoMonId = boMonId ?? 0, ChucDanh = "Giảng viên" });
    }

    // POST: /Admin/GiaoVien/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> Create(GiaoVienCreateVM vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.MaGiaoVien) && await _giaoVienService.MaGiaoVienExistsAsync(vm.MaGiaoVien))
        {
            ModelState.AddModelError(nameof(vm.MaGiaoVien), $"Mã giảng viên \"{vm.MaGiaoVien.Trim()}\" đã tồn tại");
        }
        if (!string.IsNullOrWhiteSpace(vm.Email) && await _giaoVienService.EmailExistsAsync(vm.Email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng cho tài khoản khác");
        }

        if (ModelState.IsValid)
        {
            var (success, id, errors) = await _giaoVienService.CreateGiaoVienAsync(vm);
            if (success)
            {
                TempData["success"] = $"Đã thêm giảng viên {vm.HoTen.Trim()} và tạo tài khoản {vm.Email.Trim()}";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Lỗi từ Identity (vd: mật khẩu chưa đủ mạnh) hoặc bộ môn không tồn tại
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        vm.MatKhau = string.Empty; // không trả lại mật khẩu về form
        await LoadBoMonList(vm.BoMonId);
        return View(vm);
    }

    // GET: /Admin/GiaoVien/Edit/5
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vm = await _giaoVienService.GetGiaoVienForEditAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }

        await LoadBoMonList(vm.BoMonId);
        return View(vm);
    }

    // POST: /Admin/GiaoVien/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> Edit([FromRoute] int id, GiaoVienEditVM vm)
    {
        vm.Id = id; // Id lấy từ URL, không tin giá trị gửi lên trong form

        if (!string.IsNullOrWhiteSpace(vm.MaGiaoVien) && await _giaoVienService.MaGiaoVienExistsAsync(vm.MaGiaoVien, id))
        {
            ModelState.AddModelError(nameof(vm.MaGiaoVien), $"Mã giảng viên \"{vm.MaGiaoVien.Trim()}\" đã tồn tại");
        }
        if (!string.IsNullOrWhiteSpace(vm.Email) && await _giaoVienService.EmailExistsAsync(vm.Email, id))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng cho tài khoản khác");
        }

        if (ModelState.IsValid)
        {
            var (success, errors) = await _giaoVienService.UpdateGiaoVienAsync(vm);
            if (success)
            {
                TempData["success"] = $"Đã cập nhật giảng viên {vm.HoTen.Trim()}";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Lỗi từ Identity (vd: mật khẩu chưa đủ mạnh) hoặc bộ môn không tồn tại; transaction chưa commit nên DB không đổi
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        vm.MatKhauMoi = null; // không trả lại mật khẩu về form
        await LoadBoMonList(vm.BoMonId);
        return View(vm);
    }

    // Dropdown bộ môn, nhóm theo khoa
    private async Task LoadBoMonList(int? selectedId)
    {
        var boMons = await _boMonService.GetAllBoMonAsync(includeKhoa: true);
        var nhom = new Dictionary<int, SelectListGroup>();
        ViewData["BoMonSelect"] = boMons.Select(b =>
        {
            if (!nhom.TryGetValue(b.KhoaId, out var group))
            {
                group = new SelectListGroup { Name = b.Khoa?.TenKhoa ?? "Khác" };
                nhom[b.KhoaId] = group;
            }
            return new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = $"{b.MaBoMon} – {b.TenBoMon}",
                Group = group,
                Selected = b.Id == selectedId
            };
        }).ToList();
    }

    #region  CALL API

    public async Task<IActionResult> GetAllGiaoVien()
    {
        var ds = await _giaoVienService.GetAllGiaoVienAsync();
        var data = ds.Select(g => new
        {
            g.Id,
            g.MaGiaoVien,
            g.HoTen,
            g.Email,
            g.SoDienThoai,
            ChucDanh = string.IsNullOrWhiteSpace(g.ChucDanh) ? "" : g.ChucDanh,
            g.BoMonId,
            g.TenBoMon,
            g.KhoaId,
            g.TenKhoa,
            g.DangHoatDong,
            TrangThai = g.DangHoatDong ? "Đang hoạt động" : "Đã khóa",
            LanDangNhapCuoi = g.LanDangNhapCuoi?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "",
            // Dùng để sắp xếp đúng theo thời gian
            LanDangNhapCuoiSort = g.LanDangNhapCuoi?.ToString("yyyyMMddHHmmss") ?? "",
            g.SoCauHoi,
            g.SoDeThi
        });
        return Json(new { data });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken"
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> DeleteGiaoVien(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (success, message) = await _giaoVienService.DeleteGiaoVienAsync(id.Value, currentUserId);
        return Json(new { success, message });
    }

    #endregion
}
