using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class LopController : Controller
{
    private readonly ILopService _lopService;
    private readonly IKhoaService _khoaService;

    public LopController(ILopService lopService, IKhoaService khoaService)
    {
        _lopService = lopService;
        _khoaService = khoaService;
    }

    // GET: /Admin/Lop – dữ liệu bảng được lop.js tải qua GetAllLop
    // GET: /Admin/Lop?khoaId=6 (khoaId: chọn sẵn khoa trong bộ lọc)
    public async Task<IActionResult> Index(int? khoaId)
    {
        await LoadKhoaList(khoaId);
        return View();
    }

    // GET: /Admin/Lop/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lop = await _lopService.GetLopByIdAsync(id.Value);
        if (lop == null)
        {
            return NotFound();
        }

        var vm = new LopDetailVM
        {
            Lop = lop,
            SinhViens = await _lopService.GetSinhViensByLopAsync(lop.Id)
        };
        return View(vm);
    }

    // GET: /Admin/Lop/Create
    public async Task<IActionResult> Create()
    {
        await LoadKhoaList();
        return View(new Lop());
    }

    // POST: /Admin/Lop/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lop lop)
    {
        if (lop.KhoaId > 0 && await _khoaService.GetKhoaByIdAsync(lop.KhoaId) == null)
        {
            ModelState.AddModelError(nameof(Lop.KhoaId), "Vui lòng chọn khoa hợp lệ");
        }

        if (!string.IsNullOrWhiteSpace(lop.MaLop) && await _lopService.MaLopExistsAsync(lop.MaLop))
        {
            ModelState.AddModelError(nameof(Lop.MaLop), $"Mã lớp \"{lop.MaLop.Trim()}\" đã tồn tại");
        }

        if (!ModelState.IsValid)
        {
            await LoadKhoaList(lop.KhoaId);
            return View(lop);
        }

        await _lopService.CreateLopAsync(lop);
        TempData["success"] = "Thêm lớp thành công";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Lop/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lop = await _lopService.GetLopByIdAsync(id.Value);
        if (lop == null)
        {
            return NotFound();
        }

        await LoadKhoaList(lop.KhoaId);
        return View(lop);
    }

    // POST: /Admin/Lop/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, Lop lop)
    {
        if (id != lop.Id)
        {
            return NotFound();
        }

        if (lop.KhoaId > 0 && await _khoaService.GetKhoaByIdAsync(lop.KhoaId) == null)
        {
            ModelState.AddModelError(nameof(Lop.KhoaId), "Vui lòng chọn khoa hợp lệ");
        }

        if (!string.IsNullOrWhiteSpace(lop.MaLop) && await _lopService.MaLopExistsAsync(lop.MaLop, lop.Id))
        {
            ModelState.AddModelError(nameof(Lop.MaLop), $"Mã lớp \"{lop.MaLop.Trim()}\" đã tồn tại");
        }

        if (!ModelState.IsValid)
        {
            await LoadKhoaList(lop.KhoaId);
            return View(lop);
        }

        if (!await _lopService.UpdateLopAsync(lop))
        {
            return NotFound();
        }

        TempData["success"] = "Cập nhật lớp thành công";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Lop/PhanSinhVien/5 – phân sinh viên vào lớp (danh sách ứng viên do lop-phansinhvien.js tải)
    public async Task<IActionResult> PhanSinhVien(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lop = await _lopService.GetLopByIdAsync(id.Value);
        if (lop == null)
        {
            return NotFound();
        }

        var vm = new PhanSinhVienVaoLopVM
        {
            Lop = lop,
            SinhVienTrongLop = await _lopService.GetSinhVienChonByLopAsync(lop.Id),
            LopKhac = await _lopService.GetLopKhacAsync(lop.Id),
            // Cảnh báo của lần phân lớp vừa thực hiện (đặt trong POST, hiện sau redirect)
            CanhBao = TempData["phanLopCanhBao"] is string json && !string.IsNullOrEmpty(json)
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>()
                : new List<string>()
        };
        return View(vm);
    }

    // POST: /Admin/Lop/ThemSinhVienVaoLop – đưa sinh viên của lớp khác vào lớp này
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemSinhVienVaoLop(int lopId, int[]? sinhVienIds)
    {
        if (lopId <= 0)
        {
            return NotFound();
        }

        GhiKetQua(await _lopService.ChuyenSinhVienVaoLopAsync(lopId, sinhVienIds ?? Array.Empty<int>()));
        return RedirectToAction(nameof(PhanSinhVien), new { id = lopId });
    }

    // POST: /Admin/Lop/ChuyenSinhVienSangLop – chuyển sinh viên của lớp này sang một lớp khác
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChuyenSinhVienSangLop(int lopId, int lopDichId, int[]? sinhVienIds)
    {
        if (lopId <= 0)
        {
            return NotFound();
        }

        if (lopDichId <= 0)
        {
            TempData["error"] = "Vui lòng chọn lớp cần chuyển sinh viên sang";
            return RedirectToAction(nameof(PhanSinhVien), new { id = lopId });
        }

        if (lopDichId == lopId)
        {
            TempData["error"] = "Lớp chuyển đến phải khác lớp hiện tại";
            return RedirectToAction(nameof(PhanSinhVien), new { id = lopId });
        }

        GhiKetQua(await _lopService.ChuyenSinhVienVaoLopAsync(lopDichId, sinhVienIds ?? Array.Empty<int>()));
        return RedirectToAction(nameof(PhanSinhVien), new { id = lopId });
    }

    // Kết quả phân lớp -> toast (thành công/lỗi) + danh sách cảnh báo hiện trên trang sau redirect
    private void GhiKetQua(PhanLopResult kq)
    {
        if (kq.Success)
        {
            TempData["success"] = kq.Message;
        }
        else
        {
            TempData["error"] = kq.Message;
        }

        if (kq.CanhBao.Count > 0)
        {
            TempData["phanLopCanhBao"] = System.Text.Json.JsonSerializer.Serialize(kq.CanhBao);
        }
    }

    private async Task LoadKhoaList(int? selectedId = null)
    {
        var khoas = await _khoaService.GetAllKhoaByAsync();
        ViewData["KhoaList"] = new SelectList(khoas, nameof(Khoa.Id), nameof(Khoa.TenKhoa), selectedId);
    }

    #region CALL API

    public async Task<IActionResult> GetAllLop()
    {
        var lops = await _lopService.GetAllLopAsync();
        var soSinhVien = await _lopService.CountSinhVienByLopAsync();

        var data = lops.Select(l => new
        {
            l.Id,
            l.MaLop,
            l.TenLop,
            l.KhoaId,
            TenKhoa = l.Khoa?.TenKhoa ?? "",
            SoSinhVien = soSinhVien.GetValueOrDefault(l.Id)
        });
        return Json(new { data });
    }

    // Ứng viên để phân vào lớp: sinh viên đang thuộc lớp khác
    public async Task<IActionResult> GetSinhVienNgoaiLop(int? lopId)
    {
        if (lopId == null || lopId <= 0)
        {
            return Json(new { data = Array.Empty<object>() });
        }

        var sinhViens = await _lopService.GetSinhVienNgoaiLopAsync(lopId.Value);
        var data = sinhViens.Select(s => new
        {
            s.Id,
            s.MaSinhVien,
            s.HoTen,
            NgaySinh = s.NgaySinh.ToString("dd/MM/yyyy"),
            GioiTinh = s.GioiTinh ? "Nam" : "Nữ",
            s.Email,
            s.LopId,
            s.MaLop,
            s.TenLop,
            s.KhoaId,
            s.TenKhoa
        });
        return Json(new { data });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ lop.js
    public async Task<IActionResult> DeleteLop(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        var (success, message) = await _lopService.DeleteLopAsync(id.Value);
        return Json(new { success, message });
    }

    #endregion
}
