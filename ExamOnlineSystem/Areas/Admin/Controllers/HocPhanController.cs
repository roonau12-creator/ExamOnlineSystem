using ClosedXML.Excel;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," +SD.Role_Teacher)]
public class HocPhanController : Controller
{
    private readonly IHocPhanService _hocPhanService;
    private readonly IKhoaService _khoaService;

    public HocPhanController(IHocPhanService hocPhanService, IKhoaService khoaService)
    {
        _hocPhanService = hocPhanService;
        _khoaService = khoaService;
    }

    // GET: /HocPhan
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var hocPhans = await _hocPhanService.GetAllHocPhanAsync();
        return View(hocPhans);
    }

    public async Task<IActionResult> Create()
    {
        await LoadDropdowns(null, null);
        return View(new HocPhan());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HocPhan hocPhan)
    {
        Normalize(hocPhan);
        await ValidateHocPhan(hocPhan);
        if (!ModelState.IsValid)
        {
            await LoadDropdowns(hocPhan.KhoaId, hocPhan.MonHocId);
            return View(hocPhan);
        }

        await _hocPhanService.CreateHocPhanAsync(hocPhan);
        TempData["success"] = "Thêm học phần thành công";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hocPhan = await _hocPhanService.GetHocPhanByIdAsync(id.Value);
        if (hocPhan == null)
        {
            return NotFound();
        }

        await LoadDropdowns(hocPhan.KhoaId, hocPhan.MonHocId);
        return View(hocPhan);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HocPhan hocPhan)
    {
        if (id != hocPhan.Id)
        {
            return NotFound();
        }

        Normalize(hocPhan);
        await ValidateHocPhan(hocPhan);
        if (!ModelState.IsValid)
        {
            await LoadDropdowns(hocPhan.KhoaId, hocPhan.MonHocId);
            return View(hocPhan);
        }

        if (!await _hocPhanService.ExistsAsync(id))
        {
            return NotFound();
        }

        await _hocPhanService.UpdateHocPhanAsync(hocPhan);
        TempData["success"] = "Cập nhật học phần thành công";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hocPhan = await _hocPhanService.GetHocPhanByIdAsync(id.Value);
        if (hocPhan == null)
        {
            return NotFound();
        }

        return View(hocPhan);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await _hocPhanService.ExistsAsync(id))
        {
            TempData["error"] = "Không tìm thấy học phần cần xóa";
            return RedirectToAction("Index");
        }

        await _hocPhanService.DeleteHocPhanAsync(id);
        TempData["success"] = "Xóa học phần thành công";
        return RedirectToAction("Index");
    }

    // GET: /HocPhan/GetMonHocByKhoa?khoaId=1 — dùng cho dropdown môn học phụ thuộc khoa
    [HttpGet]
    public async Task<IActionResult> GetMonHocByKhoa(int khoaId)
    {
        var monHocs = await _hocPhanService.GetMonHocByKhoaAsync(khoaId);
        return Json(monHocs.Select(m => new { id = m.Id, ten = m.MaMonHoc + " - " + m.TenMonHoc }));
    }

    // ===== Nhập học phần từ Excel =====
    // Cấu trúc file: dòng 1 là tiêu đề, dữ liệu từ dòng 2:
    // A = Mã học phần, B = Tên học phần, C = Mã môn học, D = Số tín chỉ.
    // Khoa được lấy theo môn học. Mã học phần đã tồn tại -> cập nhật; chưa có -> thêm mới.

    private record ImportRow(string Ma, string Ten, int KhoaId, int MonHocId, int SoTinChi);

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

        var monHocs = (await _hocPhanService.GetAllMonHocAsync()).ToList();
        var errors = new List<string>();
        var rows = new List<ImportRow>();

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
                var maMonHoc = sheet.Cell(r, 3).GetFormattedString().Trim();
                var tinChiText = sheet.Cell(r, 4).GetFormattedString().Trim();

                if (ma == "" && ten == "" && maMonHoc == "" && tinChiText == "")
                {
                    continue; // bỏ qua dòng trống
                }

                var ok = true;
                if (ma == "") { errors.Add($"Dòng {r}: thiếu mã học phần"); ok = false; }
                if (ten == "") { errors.Add($"Dòng {r}: thiếu tên học phần"); ok = false; }

                MonHoc? monHoc = null;
                if (maMonHoc == "")
                {
                    errors.Add($"Dòng {r}: thiếu mã môn học"); ok = false;
                }
                else
                {
                    monHoc = monHocs.FirstOrDefault(m => m.MaMonHoc.Equals(maMonHoc, StringComparison.OrdinalIgnoreCase));
                    if (monHoc == null) { errors.Add($"Dòng {r}: không tìm thấy môn học có mã \"{maMonHoc}\""); ok = false; }
                }

                if (!int.TryParse(tinChiText, out var soTinChi) || soTinChi < 1 || soTinChi > 20)
                {
                    errors.Add($"Dòng {r}: số tín chỉ \"{tinChiText}\" không hợp lệ (số nguyên từ 1 đến 20)"); ok = false;
                }

                if (ma != "" && rows.Any(x => x.Ma.Equals(ma, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add($"Dòng {r}: mã học phần \"{ma}\" bị trùng trong file"); ok = false;
                }

                if (ok)
                {
                    rows.Add(new ImportRow(ma, ten, monHoc!.KhoaId, monHoc.Id, soTinChi));
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
            ModelState.AddModelError(string.Empty, "File không có dữ liệu học phần nào");
            return View();
        }

        var (added, updated) = await _hocPhanService.ImportHocPhanAsync(rows.Select(x => new HocPhan
        {
            MaHocPhan = x.Ma, TenHocPhan = x.Ten, KhoaId = x.KhoaId, MonHocId = x.MonHocId, SoTinChi = x.SoTinChi
        }));
        TempData["success"] = $"Nhập Excel thành công: thêm mới {added}, cập nhật {updated} học phần";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> DownloadTemplate()
    {
        var monHocs = (await _hocPhanService.GetAllMonHocAsync()).ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("HocPhan");
        sheet.Cell(1, 1).Value = "Mã học phần";
        sheet.Cell(1, 2).Value = "Tên học phần";
        sheet.Cell(1, 3).Value = "Mã môn học";
        sheet.Cell(1, 4).Value = "Số tín chỉ";
        sheet.Column(1).Style.NumberFormat.Format = "@"; // giữ số 0 đầu
        sheet.Column(3).Style.NumberFormat.Format = "@";
        sheet.Cell(2, 1).Value = "HP01";
        sheet.Cell(2, 2).Value = "Tên học phần mẫu";
        sheet.Cell(2, 3).Value = monHocs.FirstOrDefault()?.MaMonHoc ?? "";
        sheet.Cell(2, 4).Value = 3;
        StyleHeader(sheet.Range(1, 1, 1, 4));

        // Sheet tham khảo: danh sách môn học hiện có
        var ref_ = workbook.Worksheets.Add("DanhSachMonHoc");
        ref_.Cell(1, 1).Value = "Mã môn học";
        ref_.Cell(1, 2).Value = "Tên môn học";
        ref_.Cell(1, 3).Value = "Khoa";
        ref_.Column(1).Style.NumberFormat.Format = "@";
        for (var i = 0; i < monHocs.Count; i++)
        {
            ref_.Cell(i + 2, 1).Value = monHocs[i].MaMonHoc;
            ref_.Cell(i + 2, 2).Value = monHocs[i].TenMonHoc;
            ref_.Cell(i + 2, 3).Value = monHocs[i].Khoa?.TenKhoa;
        }
        StyleHeader(ref_.Range(1, 1, 1, 3));
        ref_.Columns().AdjustToContents();

        // Dropdown chọn mã môn học ngay trong file mẫu
        if (monHocs.Count > 0)
        {
            sheet.Range("C2:C1000").CreateDataValidation().List(ref_.Range(2, 1, monHocs.Count + 1, 1));
        }
        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapHocPhan.xlsx");
    }

    private static void StyleHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
    }

    private static void Normalize(HocPhan hocPhan)
    {
        hocPhan.MaHocPhan = hocPhan.MaHocPhan?.Trim() ?? string.Empty;
        hocPhan.TenHocPhan = hocPhan.TenHocPhan?.Trim() ?? string.Empty;
    }

    private async Task ValidateHocPhan(HocPhan hocPhan)
    {
        if (await _khoaService.GetKhoaByIdAsync(hocPhan.KhoaId) == null)
        {
            ModelState.AddModelError(nameof(HocPhan.KhoaId), "Vui lòng chọn khoa hợp lệ");
        }

        // Môn học phải tồn tại và thuộc khoa đã chọn
        if (!await _hocPhanService.MonHocThuocKhoaAsync(hocPhan.MonHocId, hocPhan.KhoaId))
        {
            ModelState.AddModelError(nameof(HocPhan.MonHocId), "Vui lòng chọn môn học thuộc khoa đã chọn");
        }

        if (!string.IsNullOrEmpty(hocPhan.MaHocPhan))
        {
            if (await _hocPhanService.IsMaHocPhanDuplicateAsync(hocPhan.MaHocPhan, hocPhan.Id))
            {
                ModelState.AddModelError(nameof(HocPhan.MaHocPhan), "Mã học phần đã tồn tại");
            }
        }
    }

    // Đổ dữ liệu cho dropdown; danh sách môn học chỉ gồm môn học của khoa đang chọn
    private async Task LoadDropdowns(int? khoaId, int? monHocId)
    {
        var khoas = await _khoaService.GetAllKhoaByAsync();
        ViewData["KhoaList"] = new SelectList(khoas, nameof(Khoa.Id), nameof(Khoa.TenKhoa), khoaId);

        var monHocs = khoaId is > 0
            ? (await _hocPhanService.GetMonHocByKhoaAsync(khoaId.Value))
                .Select(m => new { m.Id, Ten = m.MaMonHoc + " - " + m.TenMonHoc })
                .ToList()
            : [];
        ViewData["MonHocList"] = new SelectList(monHocs, "Id", "Ten", monHocId);
    }
}
