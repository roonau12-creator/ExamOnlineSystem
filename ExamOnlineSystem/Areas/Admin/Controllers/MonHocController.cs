using ClosedXML.Excel;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
public class MonHocController : Controller
{
    private readonly ApplicationDbContext _context;

    public MonHocController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /MonHoc
    public async Task<IActionResult> Index()
    {
        var monHocs = await _context.MonHocs
            .Include(m => m.Khoa)
            .Include(m => m.BoMon)
            .OrderBy(m => m.Khoa!.SoThuTu)
            .ThenBy(m => m.BoMon!.SoThuTu)
            .ThenBy(m => m.SoThuTu)
            .AsNoTracking()
            .ToListAsync();
        return View(monHocs);
    }

    public async Task<IActionResult> Create()
    {
        await LoadDropdowns(null, null);
        return View(new MonHoc());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MonHoc monHoc)
    {
        Normalize(monHoc);
        await ValidateMonHoc(monHoc);
        if (!ModelState.IsValid)
        {
            await LoadDropdowns(monHoc.KhoaId, monHoc.BoMonId);
            return View(monHoc);
        }

        _context.MonHocs.Add(monHoc);
        await _context.SaveChangesAsync();
        TempData["success"] = "Thêm môn học thành công";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var monHoc = await _context.MonHocs.FindAsync(id);
        if (monHoc == null)
        {
            return NotFound();
        }

        await LoadDropdowns(monHoc.KhoaId, monHoc.BoMonId);
        return View(monHoc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MonHoc monHoc)
    {
        if (id != monHoc.Id)
        {
            return NotFound();
        }

        Normalize(monHoc);
        await ValidateMonHoc(monHoc);
        if (!ModelState.IsValid)
        {
            await LoadDropdowns(monHoc.KhoaId, monHoc.BoMonId);
            return View(monHoc);
        }

        if (!await _context.MonHocs.AnyAsync(m => m.Id == id))
        {
            return NotFound();
        }

        _context.MonHocs.Update(monHoc);
        await _context.SaveChangesAsync();
        TempData["success"] = "Cập nhật môn học thành công";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var monHoc = await _context.MonHocs
            .Include(m => m.Khoa)
            .Include(m => m.BoMon)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);
        if (monHoc == null)
        {
            return NotFound();
        }

        return View(monHoc);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var monHoc = await _context.MonHocs.FindAsync(id);
        if (monHoc == null)
        {
            TempData["error"] = "Không tìm thấy môn học cần xóa";
            return RedirectToAction("Index");
        }

        _context.MonHocs.Remove(monHoc);
        await _context.SaveChangesAsync();
        TempData["success"] = "Xóa môn học thành công";
        return RedirectToAction("Index");
    }

    // GET: /MonHoc/GetBoMonByKhoa?khoaId=1 — dùng cho dropdown bộ môn phụ thuộc khoa
    [HttpGet]
    public async Task<IActionResult> GetBoMonByKhoa(int khoaId)
    {
        var boMons = await _context.BoMons
            .Where(b => b.KhoaId == khoaId)
            .OrderBy(b => b.SoThuTu)
            .Select(b => new { id = b.Id, ten = b.TenBoMon })
            .AsNoTracking()
            .ToListAsync();
        return Json(boMons);
    }

    // ===== Nhập môn học từ Excel =====
    // Cấu trúc file: dòng 1 là tiêu đề, dữ liệu từ dòng 2:
    // A = Mã môn học, B = Tên môn học, C = Mã bộ môn, D = Hình thức thi, E = Thời gian thi (phút),
    // F = Số thứ tự, G = Trạng thái (để trống = "Hoạt động"), H = Mô tả (không bắt buộc),
    // I = Tên tiếng Anh (không bắt buộc; đặt cuối để file mẫu cũ vẫn dùng được).
    // Khoa được lấy theo bộ môn. Mã môn học đã tồn tại -> cập nhật; chưa có -> thêm mới.

    private record ImportRow(string Ma, string Ten, int KhoaId, int BoMonId, string HinhThuc,
        int ThoiGian, int Stt, string TrangThai, string MoTa, string TenTiengAnh);

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

        var boMons = await _context.BoMons.AsNoTracking().ToListAsync();
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
                var cells = Enumerable.Range(1, 9)
                    .Select(c => sheet.Cell(r, c).GetFormattedString().Trim())
                    .ToArray();
                if (cells.All(c => c == ""))
                {
                    continue; // bỏ qua dòng trống
                }

                var (ma, ten, maBoMon, hinhThucText, thoiGianText, sttText, trangThaiText, moTa) =
                    (cells[0], cells[1], cells[2], cells[3], cells[4], cells[5], cells[6], cells[7]);
                var tenTiengAnh = cells[8];
                var ok = true;

                if (ma == "") { errors.Add($"Dòng {r}: thiếu mã môn học"); ok = false; }
                if (ten == "") { errors.Add($"Dòng {r}: thiếu tên môn học"); ok = false; }

                BoMon? boMon = null;
                if (maBoMon == "")
                {
                    errors.Add($"Dòng {r}: thiếu mã bộ môn"); ok = false;
                }
                else
                {
                    boMon = boMons.FirstOrDefault(b => b.MaBoMon.Equals(maBoMon, StringComparison.OrdinalIgnoreCase));
                    if (boMon == null) { errors.Add($"Dòng {r}: không tìm thấy bộ môn có mã \"{maBoMon}\""); ok = false; }
                }

                var hinhThuc = MatchOption(MonHoc.DanhSachHinhThucThi, hinhThucText);
                if (hinhThuc == null)
                {
                    errors.Add($"Dòng {r}: hình thức thi \"{hinhThucText}\" không hợp lệ (chọn: {string.Join(", ", MonHoc.DanhSachHinhThucThi)})");
                    ok = false;
                }

                if (!int.TryParse(thoiGianText, out var thoiGian) || thoiGian < 1 || thoiGian > 600)
                {
                    errors.Add($"Dòng {r}: thời gian thi \"{thoiGianText}\" không hợp lệ (số nguyên từ 1 đến 600)"); ok = false;
                }

                if (!int.TryParse(sttText, out var stt) || stt < 1)
                {
                    errors.Add($"Dòng {r}: số thứ tự \"{sttText}\" không hợp lệ (phải là số nguyên > 0)"); ok = false;
                }

                var trangThai = trangThaiText == "" ? MonHoc.DanhSachTrangThai[0] : MatchOption(MonHoc.DanhSachTrangThai, trangThaiText);
                if (trangThai == null)
                {
                    errors.Add($"Dòng {r}: trạng thái \"{trangThaiText}\" không hợp lệ (chọn: {string.Join(", ", MonHoc.DanhSachTrangThai)})");
                    ok = false;
                }

                if (ma != "" && rows.Any(x => x.Ma.Equals(ma, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add($"Dòng {r}: mã môn học \"{ma}\" bị trùng trong file"); ok = false;
                }

                if (ok)
                {
                    rows.Add(new ImportRow(ma, ten, boMon!.KhoaId, boMon.Id, hinhThuc!, thoiGian, stt, trangThai!, moTa, tenTiengAnh));
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
            ModelState.AddModelError(string.Empty, "File không có dữ liệu môn học nào");
            return View();
        }

        var existing = await _context.MonHocs.ToListAsync();
        int added = 0, updated = 0;
        foreach (var row in rows)
        {
            var monHoc = existing.FirstOrDefault(m => m.MaMonHoc.Equals(row.Ma, StringComparison.OrdinalIgnoreCase));
            if (monHoc == null)
            {
                monHoc = new MonHoc { MaMonHoc = row.Ma };
                _context.MonHocs.Add(monHoc);
                added++;
            }
            else
            {
                updated++;
            }

            monHoc.TenMonHoc = row.Ten;
            monHoc.TenTiengAnh = row.TenTiengAnh;
            monHoc.KhoaId = row.KhoaId;
            monHoc.BoMonId = row.BoMonId;
            monHoc.HinhThucThi = row.HinhThuc;
            monHoc.ThoiGianThi = row.ThoiGian;
            monHoc.SoThuTu = row.Stt;
            monHoc.TrangThai = row.TrangThai;
            monHoc.MoTa = row.MoTa;
        }

        await _context.SaveChangesAsync();
        TempData["success"] = $"Nhập Excel thành công: thêm mới {added}, cập nhật {updated} môn học";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> DownloadTemplate()
    {
        var boMons = await _context.BoMons
            .Include(b => b.Khoa)
            .OrderBy(b => b.Khoa!.SoThuTu)
            .ThenBy(b => b.SoThuTu)
            .AsNoTracking()
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("MonHoc");
        string[] headers = { "Mã môn học", "Tên môn học", "Mã bộ môn", "Hình thức thi", "Thời gian thi (phút)", "Số thứ tự", "Trạng thái", "Mô tả", "Tên tiếng Anh" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }
        sheet.Column(1).Style.NumberFormat.Format = "@"; // giữ số 0 đầu
        sheet.Column(3).Style.NumberFormat.Format = "@";
        sheet.Cell(2, 1).Value = "MH01";
        sheet.Cell(2, 2).Value = "Tên môn học mẫu";
        sheet.Cell(2, 3).Value = boMons.FirstOrDefault()?.MaBoMon ?? "";
        sheet.Cell(2, 4).Value = MonHoc.DanhSachHinhThucThi[0];
        sheet.Cell(2, 5).Value = 60;
        sheet.Cell(2, 6).Value = 1;
        sheet.Cell(2, 7).Value = MonHoc.DanhSachTrangThai[0];
        sheet.Cell(2, 8).Value = "Mô tả (không bắt buộc)";
        sheet.Cell(2, 9).Value = "Subject name (optional)";
        StyleHeader(sheet.Range(1, 1, 1, headers.Length));

        // Sheet tham khảo: danh sách bộ môn + các giá trị hợp lệ
        var ref_ = workbook.Worksheets.Add("ThamKhao");
        ref_.Cell(1, 1).Value = "Mã bộ môn";
        ref_.Cell(1, 2).Value = "Tên bộ môn";
        ref_.Cell(1, 3).Value = "Khoa";
        ref_.Column(1).Style.NumberFormat.Format = "@";
        for (var i = 0; i < boMons.Count; i++)
        {
            ref_.Cell(i + 2, 1).Value = boMons[i].MaBoMon;
            ref_.Cell(i + 2, 2).Value = boMons[i].TenBoMon;
            ref_.Cell(i + 2, 3).Value = boMons[i].Khoa?.TenKhoa;
        }
        ref_.Cell(1, 5).Value = "Hình thức thi";
        for (var i = 0; i < MonHoc.DanhSachHinhThucThi.Length; i++)
        {
            ref_.Cell(i + 2, 5).Value = MonHoc.DanhSachHinhThucThi[i];
        }
        ref_.Cell(1, 6).Value = "Trạng thái";
        for (var i = 0; i < MonHoc.DanhSachTrangThai.Length; i++)
        {
            ref_.Cell(i + 2, 6).Value = MonHoc.DanhSachTrangThai[i];
        }
        StyleHeader(ref_.Range(1, 1, 1, 3));
        StyleHeader(ref_.Range(1, 5, 1, 6));
        ref_.Columns().AdjustToContents();

        // Dropdown chọn giá trị ngay trong file mẫu
        sheet.Range("D2:D1000").CreateDataValidation().List(ref_.Range(2, 5, MonHoc.DanhSachHinhThucThi.Length + 1, 5));
        sheet.Range("G2:G1000").CreateDataValidation().List(ref_.Range(2, 6, MonHoc.DanhSachTrangThai.Length + 1, 6));
        if (boMons.Count > 0)
        {
            sheet.Range("C2:C1000").CreateDataValidation().List(ref_.Range(2, 1, boMons.Count + 1, 1));
        }
        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapMonHoc.xlsx");
    }

    private static void StyleHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
    }

    // So khớp không phân biệt hoa thường, trả về giá trị chuẩn trong danh sách
    private static string? MatchOption(IEnumerable<string> options, string value)
    {
        return options.FirstOrDefault(o => o.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static void Normalize(MonHoc monHoc)
    {
        monHoc.MaMonHoc = monHoc.MaMonHoc?.Trim() ?? string.Empty;
        monHoc.TenMonHoc = monHoc.TenMonHoc?.Trim() ?? string.Empty;
        monHoc.TenTiengAnh = monHoc.TenTiengAnh?.Trim() ?? string.Empty;
        monHoc.MoTa = monHoc.MoTa?.Trim() ?? string.Empty;
    }

    private async Task ValidateMonHoc(MonHoc monHoc)
    {
        if (!await _context.Khoas.AnyAsync(k => k.Id == monHoc.KhoaId))
        {
            ModelState.AddModelError(nameof(MonHoc.KhoaId), "Vui lòng chọn khoa hợp lệ");
        }

        // Bộ môn phải tồn tại và thuộc khoa đã chọn
        if (!await _context.BoMons.AnyAsync(b => b.Id == monHoc.BoMonId && b.KhoaId == monHoc.KhoaId))
        {
            ModelState.AddModelError(nameof(MonHoc.BoMonId), "Vui lòng chọn bộ môn thuộc khoa đã chọn");
        }

        if (!string.IsNullOrEmpty(monHoc.MaMonHoc))
        {
            var ma = monHoc.MaMonHoc.ToLower();
            if (await _context.MonHocs.AnyAsync(m => m.Id != monHoc.Id && m.MaMonHoc.ToLower() == ma))
            {
                ModelState.AddModelError(nameof(MonHoc.MaMonHoc), "Mã môn học đã tồn tại");
            }
        }

        if (!MonHoc.DanhSachHinhThucThi.Contains(monHoc.HinhThucThi))
        {
            ModelState.AddModelError(nameof(MonHoc.HinhThucThi), "Hình thức thi không hợp lệ");
        }

        if (!MonHoc.DanhSachTrangThai.Contains(monHoc.TrangThai))
        {
            ModelState.AddModelError(nameof(MonHoc.TrangThai), "Trạng thái không hợp lệ");
        }
    }

    // Đổ dữ liệu cho các dropdown; danh sách bộ môn chỉ gồm bộ môn của khoa đang chọn
    private async Task LoadDropdowns(int? khoaId, int? boMonId)
    {
        var khoas = await _context.Khoas
            .OrderBy(k => k.SoThuTu)
            .AsNoTracking()
            .ToListAsync();
        ViewData["KhoaList"] = new SelectList(khoas, nameof(Khoa.Id), nameof(Khoa.TenKhoa), khoaId);

        var boMons = khoaId is > 0
            ? await _context.BoMons
                .Where(b => b.KhoaId == khoaId)
                .OrderBy(b => b.SoThuTu)
                .AsNoTracking()
                .ToListAsync()
            : new List<BoMon>();
        ViewData["BoMonList"] = new SelectList(boMons, nameof(BoMon.Id), nameof(BoMon.TenBoMon), boMonId);

        ViewData["HinhThucThiList"] = new SelectList(MonHoc.DanhSachHinhThucThi);
        ViewData["TrangThaiList"] = new SelectList(MonHoc.DanhSachTrangThai);
    }
}
