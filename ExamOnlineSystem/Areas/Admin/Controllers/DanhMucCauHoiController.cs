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
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class DanhMucCauHoiController : Controller
{
    private readonly IDanhMucCauHoiService _danhMucCauHoiService;

    public DanhMucCauHoiController(IDanhMucCauHoiService danhMucCauHoiService)
    {
        _danhMucCauHoiService = danhMucCauHoiService;
    }

    // GET: /Admin/DanhMucCauHoi
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Admin/DanhMucCauHoi/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var danhMuc = await _danhMucCauHoiService.GetDanhMucCauHoiDetailAsync(id.Value);
        if (danhMuc == null)
        {
            return NotFound();
        }

        var node = (await _danhMucCauHoiService.GetDanhMucTreeAsync()).FirstOrDefault(n => n.DanhMuc.Id == danhMuc.Id);
        var vm = new DanhMucCauHoiDetailVM
        {
            DanhMuc = danhMuc,
            Cap = node?.Cap ?? 1,
            ToTien = node?.ToTien ?? new(),
            DanhMucCons = await _danhMucCauHoiService.GetDanhMucConAsync(danhMuc.Id),
            CauHois = await _danhMucCauHoiService.GetCauHoisByDanhMucAsync(danhMuc.Id)
        };
        return View(vm);
    }

    // chaId (không bắt buộc): mở form tạo danh mục con, điền sẵn môn học và danh mục cha
    public async Task<IActionResult> Create(int? chaId)
    {
        var danhMuc = new DanhMucCauHoi();
        if (chaId != null)
        {
            var cha = await _danhMucCauHoiService.GetDanhMucCauHoiByIdAsync(chaId.Value);
            if (cha != null)
            {
                danhMuc.DanhMucChaId = cha.Id;
                danhMuc.MonHocId = cha.MonHocId;
            }
        }

        await LoadDropdowns(danhMuc.MonHocId > 0 ? danhMuc.MonHocId : null);
        return View(danhMuc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DanhMucCauHoi danhMuc)
    {
        await ValidateDanhMuc(danhMuc);
        if (!ModelState.IsValid)
        {
            await LoadDropdowns(danhMuc.MonHocId);
            return View(danhMuc);
        }

        await _danhMucCauHoiService.CreateDanhMucCauHoiAsync(danhMuc);
        TempData["success"] = "Thêm danh mục câu hỏi thành công";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var danhMuc = await _danhMucCauHoiService.GetDanhMucCauHoiByIdAsync(id.Value);
        if (danhMuc == null)
        {
            return NotFound();
        }

        await LoadDropdowns(danhMuc.MonHocId, danhMuc.Id);
        return View(danhMuc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DanhMucCauHoi danhMuc)
    {
        if (id != danhMuc.Id)
        {
            return NotFound();
        }

        var cu = await _danhMucCauHoiService.GetDanhMucCauHoiByIdAsync(id);
        if (cu == null)
        {
            return NotFound();
        }

        await ValidateDanhMuc(danhMuc);

        // Không được chọn chính nó hoặc danh mục con cháu của nó làm danh mục cha (tạo vòng lặp)
        if (danhMuc.DanhMucChaId != null &&
            (await _danhMucCauHoiService.GetSelfAndDescendantIdsAsync(id)).Contains(danhMuc.DanhMucChaId.Value))
        {
            ModelState.AddModelError(nameof(DanhMucCauHoi.DanhMucChaId),
                "Không thể chọn chính danh mục này hoặc danh mục con của nó làm danh mục cha");
        }

        // Đổi môn học khi còn danh mục con sẽ làm danh mục con nằm khác môn học với cha
        if (danhMuc.MonHocId != cu.MonHocId && await _danhMucCauHoiService.HasChildrenAsync(id))
        {
            ModelState.AddModelError(nameof(DanhMucCauHoi.MonHocId),
                "Danh mục đang có danh mục con nên không thể đổi môn học");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdowns(danhMuc.MonHocId, id);
            return View(danhMuc);
        }

        await _danhMucCauHoiService.UpdateDanhMucCauHoiAsync(danhMuc);
        TempData["success"] = "Cập nhật danh mục câu hỏi thành công";
        return RedirectToAction("Index");
    }

    public IActionResult Import()
    {
        return View();
    }

    // Tách đường dẫn danh mục cha "A > B > C" (chấp nhận cả "›") thành danh sách tên
    private static List<string> TachDuongDan(string duongDan) =>
        duongDan.Split(new[] { '>', '›' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

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

        var monHocs = (await _danhMucCauHoiService.GetAllMonHocAsync()).ToList();
        var errors = new List<string>();
        var rows = new List<DanhMucCauHoiImportRow>();
        var daCo = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // phát hiện dòng trùng trong file

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

            // Xác định cột theo tiêu đề ở dòng 1 (không phụ thuộc thứ tự cột);
            // nếu không nhận ra tiêu đề thì dùng thứ tự mặc định A-B-C-D.
            int colMon = 1, colTen = 2, colCha = 3, colMoTa = 4;
            var lastCol = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (var c = 1; c <= lastCol; c++)
            {
                var h = sheet.Cell(1, c).GetFormattedString().Trim().ToLowerInvariant();
                if (h.Contains("môn học")) colMon = c;
                else if (h.Contains("cha")) colCha = c;
                else if (h.Contains("tên danh mục")) colTen = c;
                else if (h.Contains("mô tả")) colMoTa = c;
            }

            for (var r = 2; r <= lastRow; r++)
            {
                var maMon = sheet.Cell(r, colMon).GetFormattedString().Trim();
                var ten = sheet.Cell(r, colTen).GetFormattedString().Trim();
                var cha = sheet.Cell(r, colCha).GetFormattedString().Trim();
                var moTa = sheet.Cell(r, colMoTa).GetFormattedString().Trim();

                if (maMon == "" && ten == "" && cha == "" && moTa == "")
                {
                    continue; // bỏ qua dòng trống
                }

                var ok = true;
                if (ten == "") { errors.Add($"Dòng {r}: thiếu tên danh mục"); ok = false; }
                if (ten.Contains('>') || ten.Contains('›'))
                {
                    errors.Add($"Dòng {r}: tên danh mục không được chứa ký tự \">\""); ok = false;
                }

                MonHoc? monHoc = null;
                if (maMon == "")
                {
                    errors.Add($"Dòng {r}: thiếu mã môn học"); ok = false;
                }
                else
                {
                    monHoc = monHocs.FirstOrDefault(m => m.MaMonHoc.Equals(maMon, StringComparison.OrdinalIgnoreCase));
                    if (monHoc == null) { errors.Add($"Dòng {r}: không tìm thấy môn học có mã \"{maMon}\""); ok = false; }
                }

                var duongDanCha = TachDuongDan(cha);
                if (ok && !daCo.Add($"{monHoc!.Id}|{string.Join(">", duongDanCha)}|{ten}"))
                {
                    errors.Add($"Dòng {r}: danh mục \"{ten}\" bị trùng trong file (cùng môn học và danh mục cha)"); ok = false;
                }

                if (ok)
                {
                    rows.Add(new DanhMucCauHoiImportRow
                    {
                        Dong = r, MonHocId = monHoc!.Id, TenDanhMuc = ten, DuongDanCha = duongDanCha, MoTa = moTa
                    });
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
            ModelState.AddModelError(string.Empty, "File không có dữ liệu danh mục nào");
            return View();
        }

        var result = await _danhMucCauHoiService.ImportDanhMucCauHoiAsync(rows);
        if (result.Errors.Count > 0)
        {
            ViewBag.Errors = result.Errors;
            return View();
        }

        TempData["success"] = $"Nhập Excel thành công: thêm mới {result.Added}, cập nhật {result.Updated} danh mục";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> DownloadTemplate()
    {
        var monHocs = (await _danhMucCauHoiService.GetAllMonHocAsync()).ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("DanhMucCauHoi");
        sheet.Cell(1, 1).Value = "Mã môn học";
        sheet.Cell(1, 2).Value = "Tên danh mục";
        sheet.Cell(1, 3).Value = "Danh mục cha";
        sheet.Cell(1, 4).Value = "Mô tả";
        sheet.Column(1).Style.NumberFormat.Format = "@"; // giữ số 0 đầu

        // Ví dụ 3 cấp: danh mục cha có thể nằm ngay trong file
        var maMau = monHocs.FirstOrDefault()?.MaMonHoc ?? "";
        object[,] mau =
        {
            { maMau, "Chương 1", "", "Danh mục gốc (để trống cột Danh mục cha)" },
            { maMau, "Bài 1.1", "Chương 1", "Danh mục cấp 2" },
            { maMau, "Phần 1.1.a", "Chương 1 > Bài 1.1", "Danh mục cấp 3: ghi đủ đường dẫn, ngăn cách bằng >" }
        };
        for (var i = 0; i < mau.GetLength(0); i++)
        {
            for (var j = 0; j < mau.GetLength(1); j++)
            {
                sheet.Cell(i + 2, j + 1).Value = mau[i, j].ToString();
            }
        }
        StyleHeader(sheet.Range(1, 1, 1, 4));
        sheet.Columns().AdjustToContents();

        // Sheet tham khảo: danh sách môn học hiện có
        var ref_ = workbook.Worksheets.Add("DanhSachMonHoc");
        ref_.Cell(1, 1).Value = "Mã môn học";
        ref_.Cell(1, 2).Value = "Tên môn học";
        ref_.Column(1).Style.NumberFormat.Format = "@";
        for (var i = 0; i < monHocs.Count; i++)
        {
            ref_.Cell(i + 2, 1).Value = monHocs[i].MaMonHoc;
            ref_.Cell(i + 2, 2).Value = monHocs[i].TenMonHoc;
        }
        StyleHeader(ref_.Range(1, 1, 1, 2));
        ref_.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapDanhMucCauHoi.xlsx");
    }

    private static void StyleHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
    }

    // Đổ dữ liệu cho dropdown Môn học và Danh mục cha.
    // editingId: khi sửa, bỏ chính danh mục đó và các danh mục con cháu khỏi danh sách cha.
    private async Task LoadDropdowns(int? monHocId = null, int? editingId = null)
    {
        var monHocs = await _danhMucCauHoiService.GetAllMonHocAsync();
        ViewData["MonHocList"] = new SelectList(monHocs, nameof(MonHoc.Id), nameof(MonHoc.TenMonHoc), monHocId);

        // Cây danh mục đầy đủ (thụt lề theo cấp); view lọc theo môn học đang chọn (danh mục cha phải cùng môn học)
        var nodes = await _danhMucCauHoiService.GetDanhMucTreeAsync();
        if (editingId != null)
        {
            var loaiTru = await _danhMucCauHoiService.GetSelfAndDescendantIdsAsync(editingId.Value);
            nodes = nodes.Where(n => !loaiTru.Contains(n.DanhMuc.Id)).ToList();
        }
        ViewData["DanhMucList"] = nodes;
    }

    private async Task ValidateDanhMuc(DanhMucCauHoi danhMuc)
    {
        if (danhMuc.MonHocId > 0 && !await _danhMucCauHoiService.MonHocExistsAsync(danhMuc.MonHocId))
        {
            ModelState.AddModelError(nameof(DanhMucCauHoi.MonHocId), "Vui lòng chọn môn học hợp lệ");
        }

        if (danhMuc.DanhMucChaId != null)
        {
            var cha = await _danhMucCauHoiService.GetDanhMucCauHoiByIdAsync(danhMuc.DanhMucChaId.Value);
            if (cha == null)
            {
                ModelState.AddModelError(nameof(DanhMucCauHoi.DanhMucChaId), "Danh mục cha không tồn tại");
            }
            else if (cha.MonHocId != danhMuc.MonHocId)
            {
                ModelState.AddModelError(nameof(DanhMucCauHoi.DanhMucChaId), "Danh mục cha phải thuộc cùng môn học");
            }
        }
    }

    #region  CALL API

    public async Task<IActionResult> GetAllDanhMucCauHoi()
    {
        // Trả về theo thứ tự cây: cha đứng ngay trước các con. ThuTu dùng để DataTable giữ thứ tự cây.
        var nodes = await _danhMucCauHoiService.GetDanhMucTreeAsync();
        var data = nodes.Select((n, i) => new
        {
            n.DanhMuc.Id,
            n.DanhMuc.TenDanhMuc,
            n.Cap,
            ThuTu = i,
            TenMonHoc = n.DanhMuc.MonHoc?.TenMonHoc ?? "",
            DuongDanCha = n.DuongDanCha,
            n.DanhMuc.MoTa
        });
        return Json(new { data });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ danhmuccauhoi.js
    public async Task<IActionResult> DeleteDanhMucCauHoi(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        if (!await _danhMucCauHoiService.ExistsAsync(id.Value))
        {
            return Json(new { success = false, message = "Không tìm thấy danh mục cần xóa" });
        }

        // Chặn xóa để không mất dữ liệu: danh mục con phải xử lý trước,
        // còn câu hỏi thì sẽ bị xóa dây chuyền (kèm đáp án) nếu cho xóa danh mục
        if (await _danhMucCauHoiService.HasChildrenAsync(id.Value))
        {
            return Json(new { success = false, message = "Danh mục đang có danh mục con, hãy xóa hoặc chuyển danh mục con trước" });
        }

        if (await _danhMucCauHoiService.HasCauHoiAsync(id.Value))
        {
            return Json(new { success = false, message = "Danh mục đang có câu hỏi, không thể xóa" });
        }

        await _danhMucCauHoiService.DeleteDanhMucCauHoiAsync(id.Value);
        return Json(new { success = true, message = "Xóa thành công" });
    }

    #endregion
}
