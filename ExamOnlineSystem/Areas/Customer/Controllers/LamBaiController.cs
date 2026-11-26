using System.Security.Claims;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Services;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Customer.Controllers;

// Làm bài thi trực tuyến của sinh viên đang đăng nhập.
// Mọi action đều làm việc trên lượt thi của chính sinh viên đó (không nhận sinhVienId từ URL).
[Area("Customer")]
[Authorize(Roles = SD.Role_Student)]
public class LamBaiController : Controller
{
    private readonly ISinhVienService _sinhVienService;
    private readonly ILamBaiService _lamBaiService;
    private readonly HinhAnhCauHoiStorage _hinhAnh;

    public LamBaiController(ISinhVienService sinhVienService, ILamBaiService lamBaiService,
        HinhAnhCauHoiStorage hinhAnh)
    {
        _sinhVienService = sinhVienService;
        _lamBaiService = lamBaiService;
        _hinhAnh = hinhAnh;
    }

    // POST: /Customer/LamBai/BatDau/5 (id = Id lịch thi) – mở lượt làm bài rồi vào trang làm bài
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatDau(int id)
    {
        var sinhVienId = await LaySinhVienIdAsync();
        if (sinhVienId == null)
        {
            return View("~/Areas/Customer/Views/HoSo/KhongCoHoSo.cshtml");
        }

        var (success, message, lanThiId) = await _lamBaiService.BatDauAsync(sinhVienId.Value, id);
        if (!success)
        {
            TempData["error"] = message;
            return RedirectToAction("XacNhan", "LichThi", new { id });
        }
        return RedirectToAction(nameof(Index), new { id = lanThiId });
    }

    // GET: /Customer/LamBai/Index/7 (id = Id lần thi) – giao diện làm bài
    public async Task<IActionResult> Index(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var sinhVienId = await LaySinhVienIdAsync();
        if (sinhVienId == null)
        {
            return View("~/Areas/Customer/Views/HoSo/KhongCoHoSo.cshtml");
        }

        var vm = await _lamBaiService.GetBaiLamAsync(sinhVienId.Value, id.Value);
        if (vm == null)
        {
            return NotFound(); // lần thi không tồn tại hoặc không phải của sinh viên này
        }
        if (vm.TrangThai != 0)
        {
            return RedirectToAction(nameof(KetQua), new { id = vm.LanThiId }); // đã nộp
        }
        return View(vm);
    }

    // POST: /Customer/LamBai/LuuCau – lưu bài làm của một câu (gọi bằng AJAX khi sinh viên chọn / nhập)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuCau(int lanThiId, int cauHoiLanThiId, List<int>? dapAnIds, string? noiDungTraLoi)
    {
        var sinhVienId = await LaySinhVienIdAsync();
        if (sinhVienId == null)
        {
            return Json(new { success = false, message = "Tài khoản của bạn chưa gắn hồ sơ sinh viên" });
        }

        var (success, message, soCauDaLam) = await _lamBaiService.LuuCauAsync(sinhVienId.Value, lanThiId,
            cauHoiLanThiId, dapAnIds ?? new List<int>(), noiDungTraLoi);
        return Json(new { success, message, soCauDaLam });
    }

    // POST: /Customer/LamBai/NopBai/7 (id = Id lần thi); tuDong = true khi hết giờ
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NopBai(int id, bool tuDong = false)
    {
        var sinhVienId = await LaySinhVienIdAsync();
        if (sinhVienId == null)
        {
            return View("~/Areas/Customer/Views/HoSo/KhongCoHoSo.cshtml");
        }

        var (success, message) = await _lamBaiService.NopBaiAsync(sinhVienId.Value, id, tuDong);
        if (!success)
        {
            TempData["error"] = message;
            return RedirectToAction(nameof(Index), new { id });
        }

        TempData["success"] = message;
        return RedirectToAction(nameof(KetQua), new { id });
    }

    // POST: /Customer/LamBai/ViPham/7 – trang làm bài báo sinh viên mở / chuyển sang tab khác (AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ViPham(int id)
    {
        var sinhVienId = await LaySinhVienIdAsync();
        if (sinhVienId == null)
        {
            return Json(new { success = false, message = "Tài khoản của bạn chưa gắn hồ sơ sinh viên" });
        }

        var (success, message) = await _lamBaiService.DinhChiAsync(sinhVienId.Value, id);
        return Json(new { success, message });
    }

    // GET: /Customer/LamBai/KetQua/7 – kết quả ngay sau khi nộp bài
    public async Task<IActionResult> KetQua(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var sinhVienId = await LaySinhVienIdAsync();
        if (sinhVienId == null)
        {
            return View("~/Areas/Customer/Views/HoSo/KhongCoHoSo.cshtml");
        }

        var vm = await _lamBaiService.GetKetQuaAsync(sinhVienId.Value, id.Value);
        if (vm == null)
        {
            // Chưa nộp thì về lại trang làm bài
            return RedirectToAction(nameof(Index), new { id });
        }
        return View(vm);
    }

    // GET: /Customer/LamBai/HinhAnh/12 (id = Id câu hỏi trong lần thi) – ảnh minh họa của câu hỏi
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> HinhAnh(int id)
    {
        var sinhVienId = await LaySinhVienIdAsync();
        if (sinhVienId == null)
        {
            return NotFound();
        }

        var tenFile = await _lamBaiService.GetTenFileHinhAnhAsync(sinhVienId.Value, id);
        var duongDan = _hinhAnh.DuongDan(tenFile);
        if (duongDan == null || !System.IO.File.Exists(duongDan))
        {
            return NotFound();
        }
        return PhysicalFile(duongDan, HinhAnhCauHoiStorage.ContentType(tenFile!));
    }

    private async Task<int?> LaySinhVienIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId == null ? null : await _sinhVienService.GetSinhVienIdByUserIdAsync(userId);
    }
}
