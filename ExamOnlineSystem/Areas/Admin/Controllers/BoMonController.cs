using ClosedXML.Excel;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]

public class BoMonController : Controller
{
    private readonly IBoMonService _boMonService;
    private readonly IKhoaService _khoaService;
    private readonly IGiaoVienService _giaoVienService;

    public BoMonController(IBoMonService boMonService, IKhoaService khoaService, IGiaoVienService giaoVienService)
    {
        _boMonService = boMonService;
        _khoaService = khoaService;
        _giaoVienService = giaoVienService;
    }

    // GET: /Admin/BoMon?khoaId=1 (khoaId: chọn sẵn khoa trong bộ lọc)
    public async Task<IActionResult> Index(int? khoaId)
    {
        await LoadKhoaList(khoaId);
        return View();
    }

    // GET: /Admin/BoMon/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vm = await _boMonService.GetBoMonDetailAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    // GET: /Admin/BoMon/PhanGiaoVien/5 – gán giảng viên vào bộ môn / chuyển giảng viên sang bộ môn khác
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> PhanGiaoVien(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var boMon = await _boMonService.GetBoMonByIdAsync(id.Value, includeKhoa: true);
        if (boMon == null)
        {
            return NotFound();
        }

        var giaoViens = await _giaoVienService.GetAllGiaoVienAsync();
        var vm = new PhanGiaoVienVaoBoMonVM
        {
            BoMon = boMon,
            TrongBoMon = giaoViens.Where(g => g.BoMonId == boMon.Id).OrderBy(g => g.MaGiaoVien).ToList(),
            NgoaiBoMon = giaoViens.Where(g => g.BoMonId != boMon.Id).ToList(),
            BoMonKhac = (await _boMonService.GetAllBoMonAsync(includeKhoa: true)).Where(b => b.Id != boMon.Id).ToList()
        };
        return View(vm);
    }

    // POST: /Admin/BoMon/GanGiaoVien – đưa giảng viên của bộ môn khác vào bộ môn này
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> GanGiaoVien(int boMonId, int[]? giaoVienIds)
    {
        if (boMonId <= 0)
        {
            return NotFound();
        }

        GhiKetQua(await _giaoVienService.GanGiaoVienVaoBoMonAsync(boMonId, giaoVienIds ?? Array.Empty<int>()));
        return RedirectToAction(nameof(PhanGiaoVien), new { id = boMonId });
    }

    // POST: /Admin/BoMon/ChuyenGiaoVien – chuyển giảng viên của bộ môn này sang bộ môn khác
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<IActionResult> ChuyenGiaoVien(int boMonId, int boMonDichId, int[]? giaoVienIds)
    {
        if (boMonId <= 0)
        {
            return NotFound();
        }

        if (boMonDichId <= 0 || boMonDichId == boMonId)
        {
            TempData["error"] = "Vui lòng chọn bộ môn khác để chuyển giảng viên sang";
        }
        else
        {
            GhiKetQua(await _giaoVienService.GanGiaoVienVaoBoMonAsync(boMonDichId, giaoVienIds ?? Array.Empty<int>()));
        }
        return RedirectToAction(nameof(PhanGiaoVien), new { id = boMonId });
    }

    private void GhiKetQua((bool Success, string Message) kq)
    {
        TempData[kq.Success ? "success" : "error"] = kq.Message;
    }

    public async Task<IActionResult> Create()
    {
        await LoadKhoaList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BoMon boMon)
    {
        await ValidateKhoa(boMon.KhoaId);
        if (!ModelState.IsValid)
        {
            await LoadKhoaList(boMon.KhoaId);
            return View(boMon);
        }

        await _boMonService.CreateBoMonAsync(boMon);
        TempData["success"] = "Thêm bộ môn thành công";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var boMon = await _boMonService.GetBoMonByIdAsync(id.Value);
        if (boMon == null)
        {
            return NotFound();
        }

        await LoadKhoaList(boMon.KhoaId);
        return View(boMon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BoMon boMon)
    {
        if (id != boMon.Id)
        {
            return NotFound();
        }

        await ValidateKhoa(boMon.KhoaId);
        if (!ModelState.IsValid)
        {
            await LoadKhoaList(boMon.KhoaId);
            return View(boMon);
        }

        if (!await _boMonService.ExistsAsync(id))
        {
            return NotFound();
        }

        await _boMonService.UpdateBoMonAsync(boMon);
        TempData["success"] = "Cập nhật bộ môn thành công";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var boMon = await _boMonService.GetBoMonByIdAsync(id.Value);
        if (boMon == null)
        {
            return NotFound();
        }

        return View(boMon);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await _boMonService.ExistsAsync(id))
        {
            TempData["error"] = "Không tìm thấy bộ môn cần xóa";
            return RedirectToAction("Index");
        }

        await _boMonService.DeleteBoMonAsync(id);
        TempData["success"] = "Xóa bộ môn thành công";
        return RedirectToAction("Index");
    }
    

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

        var khoas = (await _khoaService.GetAllKhoaByAsync()).ToList();
        var errors = new List<string>();
        var rows = new List<(string Ma, string Ten, int KhoaId, int Stt)>();

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

            // Xác định cột theo tiêu đề ở dòng 1 (không phụ thuộc thứ tự cột);
            // nếu không nhận ra tiêu đề thì dùng thứ tự mặc định A-B-C-D.
            int colMa = 1, colTen = 2, colKhoa = 3, colStt = 4;
            var lastCol = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (var c = 1; c <= lastCol; c++)
            {
                var h = sheet.Cell(1, c).GetFormattedString().Trim().ToLowerInvariant();
                if (h.Contains("khoa") && !h.Contains("bộ môn")) colKhoa = c;
                else if (h.Contains("mã bộ môn")) colMa = c;
                else if (h.Contains("tên bộ môn")) colTen = c;
                else if (h.Contains("thứ tự") || h == "stt") colStt = c;
            }

            for (var r = 2; r <= lastRow; r++)
            {
                var ma = sheet.Cell(r, colMa).GetFormattedString().Trim();
                var ten = sheet.Cell(r, colTen).GetFormattedString().Trim();
                var maKhoa = sheet.Cell(r, colKhoa).GetFormattedString().Trim();
                var sttText = sheet.Cell(r, colStt).GetFormattedString().Trim();

                if (ma == "" && ten == "" && maKhoa == "" && sttText == "")
                {
                    continue; // bỏ qua dòng trống
                }

                var ok = true;
                if (ma == "") { errors.Add($"Dòng {r}: thiếu mã bộ môn"); ok = false; }
                if (ten == "") { errors.Add($"Dòng {r}: thiếu tên bộ môn"); ok = false; }

                Khoa? khoa = null;
                if (maKhoa == "")
                {
                    errors.Add($"Dòng {r}: thiếu mã khoa"); ok = false;
                }
                else
                {
                    khoa = khoas.FirstOrDefault(k => k.MaKhoa.Equals(maKhoa, StringComparison.OrdinalIgnoreCase));
                    if (khoa == null) { errors.Add($"Dòng {r}: không tìm thấy khoa có mã \"{maKhoa}\""); ok = false; }
                }

                if (!int.TryParse(sttText, out var stt) || stt < 1)
                {
                    errors.Add($"Dòng {r}: số thứ tự \"{sttText}\" không hợp lệ (phải là số nguyên > 0)"); ok = false;
                }

                if (ma != "" && rows.Any(x => x.Ma.Equals(ma, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add($"Dòng {r}: mã bộ môn \"{ma}\" bị trùng trong file"); ok = false;
                }

                if (ok)
                {
                    rows.Add((ma, ten, khoa!.Id, stt));
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
            ModelState.AddModelError(string.Empty, "File không có dữ liệu bộ môn nào");
            return View();
        }

        var (added, updated) = await _boMonService.ImportBoMonAsync(
            rows.Select(x => new BoMon { MaBoMon = x.Ma, TenBoMon = x.Ten, KhoaId = x.KhoaId, SoThuTu = x.Stt }));
        TempData["success"] = $"Nhập Excel thành công: thêm mới {added}, cập nhật {updated} bộ môn";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> DownloadTemplate()
    {
        var khoas = (await _khoaService.GetAllKhoaByAsync()).ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BoMon");
        sheet.Cell(1, 3).Value = "Mã khoa";
        sheet.Cell(1, 1).Value = "Mã bộ môn";
        sheet.Cell(1, 2).Value = "Tên bộ môn";
        sheet.Cell(1, 4).Value = "Số thứ tự";
        sheet.Column(1).Style.NumberFormat.Format = "@"; // giữ số 0 đầu
        sheet.Column(3).Style.NumberFormat.Format = "@";
        sheet.Cell(2, 1).Value = "BM01";
        sheet.Cell(2, 2).Value = "Tên bộ môn mẫu";
        sheet.Cell(2, 3).Value = khoas.FirstOrDefault()?.MaKhoa ?? "";
        sheet.Cell(2, 4).Value = 1;
        StyleHeader(sheet.Range(1, 1, 1, 4));
        sheet.Columns().AdjustToContents();

        // Sheet tham khảo: danh sách khoa hiện có
        var ref_ = workbook.Worksheets.Add("DanhSachKhoa");
        ref_.Cell(1, 1).Value = "Mã khoa";
        ref_.Cell(1, 2).Value = "Tên khoa";
        ref_.Column(1).Style.NumberFormat.Format = "@";
        for (var i = 0; i < khoas.Count; i++)
        {
            ref_.Cell(i + 2, 1).Value = khoas[i].MaKhoa;
            ref_.Cell(i + 2, 2).Value = khoas[i].TenKhoa;
        }
        StyleHeader(ref_.Range(1, 1, 1, 2));
        ref_.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapBoMon.xlsx");
    }

    private static void StyleHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
    }

    // Đổ danh sách khoa vào dropdown
    private async Task LoadKhoaList(int? selectedId = null)
    {
        var khoas = await _khoaService.GetAllKhoaByAsync();
        ViewData["KhoaList"] = new SelectList(khoas, nameof(Khoa.Id), nameof(Khoa.TenKhoa), selectedId);
    }

    // Kiểm tra khoa được chọn có tồn tại hay không
    private async Task ValidateKhoa(int khoaId)
    {
        if (await _khoaService.GetKhoaByIdAsync(khoaId) == null)
        {
            ModelState.AddModelError(nameof(BoMon.KhoaId), "Vui lòng chọn khoa hợp lệ");
        }
    }

    #region  CALL API

    public async Task<IActionResult> GetAllBoMon()
    {
        var Bomon = await _boMonService.GetAllBoMonAsync(true);
        return Json(new { data = Bomon });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ bomon.js
    public async Task<IActionResult> DeleteBoMon(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        if (!await _boMonService.ExistsAsync(id.Value))
        {
            return Json(new { success = false, message = "Không tìm thấy bộ môn cần xóa" });
        }
        await _boMonService.DeleteBoMonAsync(id.Value);
        return Json(new { success = true, message = "Xóa thành công" });
    }

    #endregion
}
