using ClosedXML.Excel;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class KhoaController : Controller
{
    private readonly IKhoaService _khoaService;

    public KhoaController(IKhoaService khoaService)
    {
        _khoaService = khoaService;
    }

    // GET: /Admin/Khoa
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Admin/Khoa/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vm = await _khoaService.GetKhoaDetailAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Khoa khoa)
    {
        if (!ModelState.IsValid)
        {
            return View(khoa);
        }

        await _khoaService.CreateKhoaByAsync(khoa);
        TempData["success"] = "Thêm khoa thành công";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var khoa = await _khoaService.GetKhoaByIdAsync(id.Value);
        if (khoa == null)
        {
            return NotFound();
        }

        return View(khoa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Khoa khoa)
    {
        if (id != khoa.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(khoa);
        }

        if (await _khoaService.GetKhoaByIdAsync(id) == null)
        {
            return NotFound();
        }

        await _khoaService.UpdateKhoaAsync(khoa);
        TempData["success"] = "Cập nhật khoa thành công";
        return RedirectToAction("Index");
    }
    

    // ===== Nhập khoa từ Excel =====
    // Cấu trúc file: dòng 1 là tiêu đề; cột A = Mã khoa, B = Tên khoa, C = Số thứ tự.
    // Mã khoa đã tồn tại -> cập nhật Tên khoa và Số thứ tự; chưa có -> thêm mới.

    public IActionResult Import()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn file Excel");
            return View();
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Chỉ hỗ trợ file .xlsx");
            return View();
        }

        var errors = new List<string>();
        var rows = new List<(int Row, string Ma, string Ten, int Stt)>();

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

            for (var r = 2; r <= lastRow; r++)
            {
                var ma = sheet.Cell(r, 1).GetFormattedString().Trim();
                var ten = sheet.Cell(r, 2).GetFormattedString().Trim();
                var sttText = sheet.Cell(r, 3).GetFormattedString().Trim();

                if (ma == "" && ten == "" && sttText == "")
                {
                    continue; // bỏ qua dòng trống
                }

                if (ma == "") errors.Add($"Dòng {r}: thiếu mã khoa");
                if (ten == "") errors.Add($"Dòng {r}: thiếu tên khoa");
                if (!int.TryParse(sttText, out var stt) || stt < 1)
                {
                    errors.Add($"Dòng {r}: số thứ tự \"{sttText}\" không hợp lệ (phải là số nguyên > 0)");
                }
                else if (ma != "" && ten != "")
                {
                    if (rows.Any(x => x.Ma.Equals(ma, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"Dòng {r}: mã khoa \"{ma}\" bị trùng trong file");
                    }
                    else
                    {
                        rows.Add((r, ma, ten, stt));
                    }
                }
            }
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Không đọc được file Excel. Vui lòng kiểm tra lại định dạng file.");
            return View();
        }

        if (errors.Count > 0)
        {
            ViewBag.Errors = errors;
            return View();
        }

        if (rows.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "File không có dữ liệu khoa nào");
            return View();
        }

        var (added, updated) = await _khoaService.ImportKhoaAsync(
            rows.Select(r => new Khoa { MaKhoa = r.Ma, TenKhoa = r.Ten, SoThuTu = r.Stt }));
        TempData["success"] = $"Nhập Excel thành công: thêm mới {added}, cập nhật {updated} khoa";
        return RedirectToAction("Index");
    }

    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Khoa");
        sheet.Cell(1, 1).Value = "Mã khoa";
        sheet.Cell(1, 2).Value = "Tên khoa";
        sheet.Cell(1, 3).Value = "Số thứ tự";
        sheet.Cell(2, 1).Value = "CNTT";
        sheet.Cell(2, 2).Value = "Công nghệ thông tin";
        sheet.Cell(2, 3).Value = 1;
        sheet.Column(1).Style.NumberFormat.Format = "@"; // mã khoa dạng text để giữ số 0 đầu
        var header = sheet.Range(1, 1, 1, 3);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapKhoa.xlsx");
    }

    #region  API CALLS

    public async Task<IActionResult> GetAllKhoa()
    {
        var khoas = await _khoaService.GetAllKhoaByAsync();
        return Json(new { data = khoas });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ khoa.js
    public async Task<IActionResult> DeleteKhoa(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new {success = false,message = "id không hợp lệ"});
        }
        var khoaToBeDeleted = await _khoaService.GetKhoaByIdAsync(id.Value);
        if (khoaToBeDeleted == null)
        {
            return Json(new { success = false, message = "Lỗi trong khi xóa" });
        }
        await _khoaService.DeleteKhoaAsync(id.Value);
        return Json(new { success = true, message = "Xóa thành công" });
    }

    #endregion
}
