using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class NamHocController : Controller
{
    private readonly INamHocService _namHocService;

    public NamHocController(INamHocService namHocService)
    {
        _namHocService = namHocService;
    }

    // GET: /Admin/NamHoc – dữ liệu bảng được namhoc.js tải qua GetAllNamHoc
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Admin/NamHoc/Create
    public IActionResult Create()
    {
        // Gợi ý năm học theo niên khóa hiện tại: tháng 8 trở đi tính sang năm học mới
        var homNay = DateOnly.FromDateTime(DateTime.Now);
        var namBatDau = homNay.Month >= 8 ? homNay.Year : homNay.Year - 1;
        return View(new NamHoc
        {
            MaNamHoc = $"{namBatDau}-{namBatDau + 1}",
            TenNamHoc = $"Năm học {namBatDau} - {namBatDau + 1}",
            NgayBatDau = new DateOnly(namBatDau, 9, 1),
            NgayKetThuc = new DateOnly(namBatDau + 1, 8, 31)
        });
    }

    // POST: /Admin/NamHoc/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NamHoc namHoc)
    {
        await KiemTraAsync(namHoc);

        if (!ModelState.IsValid)
        {
            return View(namHoc);
        }

        await _namHocService.CreateNamHocAsync(namHoc);
        TempData["success"] = "Thêm năm học thành công";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/NamHoc/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var namHoc = await _namHocService.GetNamHocByIdAsync(id.Value);
        if (namHoc == null)
        {
            return NotFound();
        }

        return View(namHoc);
    }

    // POST: /Admin/NamHoc/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, NamHoc namHoc)
    {
        if (id != namHoc.Id)
        {
            return NotFound();
        }

        await KiemTraAsync(namHoc, namHoc.Id);

        if (!ModelState.IsValid)
        {
            return View(namHoc);
        }

        if (!await _namHocService.UpdateNamHocAsync(namHoc))
        {
            return NotFound();
        }

        TempData["success"] = "Cập nhật năm học thành công";
        return RedirectToAction(nameof(Index));
    }

    // Kiểm tra nghiệp vụ dùng chung cho Thêm / Sửa: trùng mã, ngày kết thúc, khoảng thời gian giao nhau
    private async Task KiemTraAsync(NamHoc namHoc, int? excludeId = null)
    {
        if (!string.IsNullOrWhiteSpace(namHoc.MaNamHoc)
            && await _namHocService.MaNamHocExistsAsync(namHoc.MaNamHoc, excludeId))
        {
            ModelState.AddModelError(nameof(NamHoc.MaNamHoc),
                $"Mã năm học \"{namHoc.MaNamHoc.Trim()}\" đã tồn tại");
        }

        // Ngày để trống / sai định dạng: model binder báo bằng tiếng Anh -> đổi sang tiếng Việt.
        // Chưa có ngày hợp lệ thì không kiểm tra tiếp để tránh báo thêm lỗi nhiễu.
        var loiNgayBatDau = DoiLoiNgaySangTiengViet(nameof(NamHoc.NgayBatDau), "Vui lòng chọn ngày bắt đầu");
        var loiNgayKetThuc = DoiLoiNgaySangTiengViet(nameof(NamHoc.NgayKetThuc), "Vui lòng chọn ngày kết thúc");
        if (loiNgayBatDau || loiNgayKetThuc)
        {
            return;
        }

        if (namHoc.NgayKetThuc <= namHoc.NgayBatDau)
        {
            ModelState.AddModelError(nameof(NamHoc.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu");
            return;
        }

        var trung = await _namHocService.GetNamHocTrungThoiGianAsync(namHoc.NgayBatDau, namHoc.NgayKetThuc, excludeId);
        if (trung != null)
        {
            ModelState.AddModelError(nameof(NamHoc.NgayBatDau),
                $"Khoảng thời gian bị trùng với năm học \"{trung.TenNamHoc}\" " +
                $"({trung.NgayBatDau:dd/MM/yyyy} - {trung.NgayKetThuc:dd/MM/yyyy})");
        }
    }

    // Nếu trường ngày đang có lỗi (binder không đọc được giá trị) thì thay bằng thông báo tiếng Việt.
    // Trả về true khi trường đó có lỗi.
    private bool DoiLoiNgaySangTiengViet(string key, string thongBao)
    {
        if (!ModelState.TryGetValue(key, out var entry) || entry.Errors.Count == 0)
        {
            return false;
        }

        entry.Errors.Clear();
        ModelState.AddModelError(key, thongBao);
        return true;
    }

    #region CALL API

    public async Task<IActionResult> GetAllNamHoc()
    {
        var namHocs = await _namHocService.GetAllNamHocAsync();
        var data = namHocs.Select(n => new
        {
            n.Id,
            n.MaNamHoc,
            n.TenNamHoc,
            NgayBatDau = n.NgayBatDau.ToString("dd/MM/yyyy"),
            NgayKetThuc = n.NgayKetThuc.ToString("dd/MM/yyyy"),
            // Khóa sắp xếp dạng yyyy-MM-dd để DataTable sắp đúng thứ tự thời gian
            NgayBatDauSort = n.NgayBatDau.ToString("yyyy-MM-dd"),
            NgayKetThucSort = n.NgayKetThuc.ToString("yyyy-MM-dd"),
            n.LaNamHienTai,
            n.GhiChu
        });
        return Json(new { data });
    }

    // Đặt năm học làm năm hiện tại (token gửi qua header "RequestVerificationToken" từ namhoc.js)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatNamHienTai(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        var (success, message) = await _namHocService.DatNamHienTaiAsync(id.Value);
        return Json(new { success, message });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ namhoc.js
    public async Task<IActionResult> DeleteNamHoc(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        var (success, message) = await _namHocService.DeleteNamHocAsync(id.Value);
        return Json(new { success, message });
    }

    #endregion
}
