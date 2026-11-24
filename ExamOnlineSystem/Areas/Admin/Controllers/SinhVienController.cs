using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class SinhVienController : Controller
{
    private readonly ISinhVienService _sinhVienService;
    private readonly ILopService _lopService;
    private readonly IKhoaService _khoaService;

    public SinhVienController(ISinhVienService sinhVienService, ILopService lopService, IKhoaService khoaService)
    {
        _sinhVienService = sinhVienService;
        _lopService = lopService;
        _khoaService = khoaService;
    }

    // GET: /Admin/SinhVien – bảng được sinhvien.js tải qua GetAllSinhVien
    // GET: /Admin/SinhVien?khoaId=6&lop=ctc1 (chọn sẵn bộ lọc khi mở từ trang chi tiết khoa / lớp)
    public async Task<IActionResult> Index(int? khoaId)
    {
        // Ô lọc "Khoa" (đủ các khoa, theo số thứ tự) và "Lớp" (lớp nhớ khoa để lọc theo khoa)
        ViewData["KhoaList"] = new SelectList(await _khoaService.GetAllKhoaByAsync(), "Id", "TenKhoa", khoaId);
        ViewData["LopList"] = (await _lopService.GetAllLopAsync()).ToList();
        return View();
    }

    // GET: /Admin/SinhVien/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vm = await _sinhVienService.GetSinhVienDetailAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    // GET: /Admin/SinhVien/Create?lopId=3 (lopId: chọn sẵn lớp khi mở từ trang chi tiết lớp)
    public async Task<IActionResult> Create(int? lopId)
    {
        await LoadLopList(lopId);
        return View(new SinhVienCreateVM { LopId = lopId ?? 0 });
    }

    // POST: /Admin/SinhVien/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SinhVienCreateVM vm)
    {
        await KiemTraForm(vm, excludeId: null);

        if (ModelState.IsValid)
        {
            var (success, errors) = await _sinhVienService.CreateSinhVienAsync(vm);
            if (success)
            {
                TempData["success"] = $"Đã thêm sinh viên {vm.HoTen.Trim()} và tạo tài khoản {vm.Email.Trim()}";
                return RedirectToAction(nameof(Index));
            }

            // Lỗi từ Identity (vd: mật khẩu chưa đủ mạnh) hoặc lớp không tồn tại
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        vm.MatKhau = string.Empty; // không trả lại mật khẩu về form
        await LoadLopList(vm.LopId);
        return View(vm);
    }

    // GET: /Admin/SinhVien/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var sv = await _sinhVienService.GetSinhVienByIdAsync(id.Value);
        if (sv == null)
        {
            return NotFound();
        }

        var vm = new SinhVienEditVM
        {
            Id = sv.Id,
            MaSinhVien = sv.MaSinhVien,
            HoTen = sv.HoTen,
            NgaySinh = sv.NgaySinh,
            GioiTinh = sv.GioiTinh,
            LopId = sv.LopId,
            Email = sv.Email,
            SoDienThoai = sv.SoDienThoai
        };
        await LoadLopList(vm.LopId);
        return View(vm);
    }

    // POST: /Admin/SinhVien/Edit/5 – [FromRoute]: id lấy từ URL, không bị ô "Id" trong form ghi đè
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, SinhVienEditVM vm)
    {
        if (id != vm.Id || await _sinhVienService.GetSinhVienByIdAsync(id) == null)
        {
            return NotFound();
        }

        await KiemTraForm(vm, excludeId: id);

        if (ModelState.IsValid)
        {
            var (success, errors) = await _sinhVienService.UpdateSinhVienAsync(vm);
            if (success)
            {
                TempData["success"] = $"Đã cập nhật sinh viên {vm.HoTen.Trim()}";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        await LoadLopList(vm.LopId);
        return View(vm);
    }

    // GET: /Admin/SinhVien/Import
    public IActionResult Import()
    {
        return View();
    }

    // POST: /Admin/SinhVien/Import
    // Cột (nhận theo tiêu đề dòng 1, không nhận ra thì theo thứ tự A..H):
    // Mã sinh viên | Họ tên | Ngày sinh | Giới tính | Mã lớp | Email | Số điện thoại | Mật khẩu (không bắt buộc)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file, string? matKhauMacDinh)
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
        var rows = new List<SinhVienImportRow>();

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

            int colMa = 1, colTen = 2, colNgay = 3, colGt = 4, colLop = 5, colEmail = 6, colSdt = 7, colMk = 8;
            var lastCol = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (var c = 1; c <= lastCol; c++)
            {
                var h = sheet.Cell(1, c).GetFormattedString().Trim().ToLowerInvariant();
                if (h.Contains("mã sinh viên") || h.Contains("mã sv") || h == "mssv") colMa = c;
                else if (h.Contains("họ") && h.Contains("tên")) colTen = c;
                else if (h.Contains("ngày sinh")) colNgay = c;
                else if (h.Contains("giới tính")) colGt = c;
                else if (h.Contains("lớp")) colLop = c;
                else if (h.Contains("email")) colEmail = c;
                else if (h.Contains("điện thoại") || h == "sđt") colSdt = c;
                else if (h.Contains("mật khẩu")) colMk = c;
            }

            var homNay = DateOnly.FromDateTime(DateTime.Today);
            for (var r = 2; r <= lastRow; r++)
            {
                var ma = sheet.Cell(r, colMa).GetFormattedString().Trim();
                var ten = Regex.Replace(sheet.Cell(r, colTen).GetFormattedString().Trim(), @"\s+", " ");
                var ngayCell = sheet.Cell(r, colNgay);
                var gt = sheet.Cell(r, colGt).GetFormattedString().Trim();
                var maLop = sheet.Cell(r, colLop).GetFormattedString().Trim();
                var email = sheet.Cell(r, colEmail).GetFormattedString().Trim();
                var sdt = DocSoDienThoai(sheet.Cell(r, colSdt));
                var mk = sheet.Cell(r, colMk).GetFormattedString().Trim();

                if (ma == "" && ten == "" && ngayCell.IsEmpty() && gt == "" && maLop == "" && email == "" && sdt == "")
                {
                    continue; // bỏ qua dòng trống
                }

                var ok = true;
                void Loi(string msg) { errors.Add($"Dòng {r}: {msg}"); ok = false; }

                if (ma == "") Loi("thiếu mã sinh viên");
                else if (rows.Any(x => x.MaSinhVien.Equals(ma, StringComparison.OrdinalIgnoreCase))) Loi($"mã sinh viên \"{ma}\" bị trùng trong file");

                if (ten == "") Loi("thiếu họ tên");

                var ngaySinh = DocNgay(ngayCell);
                if (ngaySinh == null) Loi($"ngày sinh \"{ngayCell.GetFormattedString()}\" không hợp lệ (dùng dạng dd/MM/yyyy)");
                else if (ngaySinh > homNay.AddYears(-10) || ngaySinh < homNay.AddYears(-100)) Loi($"ngày sinh {ngaySinh:dd/MM/yyyy} không hợp lý");

                bool? gioiTinh = gt.ToLowerInvariant() switch
                {
                    "nam" => true,
                    "nữ" or "nu" => false,
                    _ => null
                };
                if (gioiTinh == null) Loi($"giới tính \"{gt}\" không hợp lệ (ghi Nam hoặc Nữ)");

                if (maLop == "") Loi("thiếu mã lớp");

                if (email == "") Loi("thiếu email");
                else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)) Loi($"email \"{email}\" không hợp lệ");
                else if (rows.Any(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase))) Loi($"email \"{email}\" bị trùng trong file");

                if (!Regex.IsMatch(sdt, @"^0\d{9}$")) Loi($"số điện thoại \"{sdt}\" không hợp lệ (10 chữ số, bắt đầu bằng 0)");

                if (ok)
                {
                    rows.Add(new SinhVienImportRow
                    {
                        Dong = r,
                        MaSinhVien = ma,
                        HoTen = ten,
                        NgaySinh = ngaySinh!.Value,
                        GioiTinh = gioiTinh!.Value,
                        MaLop = maLop,
                        Email = email,
                        SoDienThoai = sdt,
                        MatKhau = mk == "" ? null : mk
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
            ModelState.AddModelError(string.Empty, "File không có dữ liệu sinh viên nào");
            return View();
        }

        var kq = await _sinhVienService.ImportSinhVienAsync(rows, string.IsNullOrWhiteSpace(matKhauMacDinh) ? null : matKhauMacDinh);
        if (kq.Errors.Count > 0)
        {
            ViewBag.Errors = kq.Errors;
            return View();
        }

        var thongBao = $"Nhập Excel thành công: thêm mới {kq.Added} sinh viên";
        if (kq.Skipped.Count > 0)
        {
            var ds = string.Join(", ", kq.Skipped.Take(10)) + (kq.Skipped.Count > 10 ? ", ..." : "");
            thongBao += $"; bỏ qua {kq.Skipped.Count} sinh viên đã có ({ds})";
        }
        TempData["success"] = thongBao;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> DownloadTemplate()
    {
        var lops = (await _lopService.GetAllLopAsync()).ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("SinhVien");
        string[] tieuDe = { "Mã sinh viên", "Họ tên", "Ngày sinh", "Giới tính", "Mã lớp", "Email", "Số điện thoại", "Mật khẩu" };
        for (var i = 0; i < tieuDe.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = tieuDe[i];
        }
        // Định dạng text: giữ số 0 đầu của mã SV / SĐT, ngày sinh nhập dạng dd/MM/yyyy
        foreach (var c in new[] { 1, 3, 5, 7, 8 })
        {
            sheet.Column(c).Style.NumberFormat.Format = "@";
        }
        sheet.Cell(2, 1).Value = "SV0001";
        sheet.Cell(2, 2).Value = "Nguyễn Văn An";
        sheet.Cell(2, 3).Value = "20/05/2004";
        sheet.Cell(2, 4).Value = "Nam";
        sheet.Cell(2, 5).Value = lops.FirstOrDefault()?.MaLop ?? "";
        sheet.Cell(2, 6).Value = "an.nv@example.vn";
        sheet.Cell(2, 7).Value = "0901234567";
        sheet.Cell(2, 8).Value = "";
        var header = sheet.Range(1, 1, 1, tieuDe.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheet.Cell(1, 8).GetComment().AddText("Không bắt buộc. Để trống thì dùng mật khẩu mặc định nhập trên trang web.");
        sheet.Columns().AdjustToContents();

        // Sheet tham khảo: danh sách lớp hiện có
        var ref_ = workbook.Worksheets.Add("DanhSachLop");
        ref_.Cell(1, 1).Value = "Mã lớp";
        ref_.Cell(1, 2).Value = "Tên lớp";
        ref_.Cell(1, 3).Value = "Khoa";
        ref_.Column(1).Style.NumberFormat.Format = "@";
        for (var i = 0; i < lops.Count; i++)
        {
            ref_.Cell(i + 2, 1).Value = lops[i].MaLop;
            ref_.Cell(i + 2, 2).Value = lops[i].TenLop;
            ref_.Cell(i + 2, 3).Value = lops[i].Khoa?.TenKhoa ?? "";
        }
        var refHeader = ref_.Range(1, 1, 1, 3);
        refHeader.Style.Font.Bold = true;
        refHeader.Style.Fill.BackgroundColor = XLColor.LightBlue;
        ref_.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapSinhVien.xlsx");
    }

    // Ngày sinh: ô kiểu ngày của Excel, hoặc chữ dạng dd/MM/yyyy, d/M/yyyy, yyyy-MM-dd
    private static DateOnly? DocNgay(IXLCell cell)
    {
        if (cell.DataType == XLDataType.DateTime)
        {
            return DateOnly.FromDateTime(cell.GetDateTime());
        }

        var text = cell.GetFormattedString().Trim();
        string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd" };
        return DateOnly.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
    }

    // SĐT nhập dạng số trong Excel sẽ mất số 0 đầu (901234567) -> thêm lại
    private static string DocSoDienThoai(IXLCell cell)
    {
        var sdt = Regex.Replace(cell.GetFormattedString(), @"[\s.\-]", "");
        if (cell.DataType == XLDataType.Number && sdt.Length == 9)
        {
            sdt = "0" + sdt;
        }
        return sdt;
    }

    // Kiểm tra dùng chung cho Thêm/Sửa: trùng mã, trùng email, ngày sinh hợp lý
    private async Task KiemTraForm(SinhVienFormVM vm, int? excludeId)
    {
        if (!string.IsNullOrWhiteSpace(vm.MaSinhVien) && await _sinhVienService.MaSinhVienExistsAsync(vm.MaSinhVien, excludeId))
        {
            ModelState.AddModelError(nameof(vm.MaSinhVien), $"Mã sinh viên \"{vm.MaSinhVien.Trim()}\" đã tồn tại");
        }

        if (!string.IsNullOrWhiteSpace(vm.Email) && await _sinhVienService.EmailExistsAsync(vm.Email, excludeId))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng");
        }

        if (vm.NgaySinh is { } ns)
        {
            var homNay = DateOnly.FromDateTime(DateTime.Today);
            if (ns > homNay.AddYears(-10) || ns < homNay.AddYears(-100))
            {
                ModelState.AddModelError(nameof(vm.NgaySinh), "Ngày sinh không hợp lệ");
            }
        }
    }

    private async Task LoadLopList(int? selectedId = null)
    {
        var lops = await _lopService.GetAllLopAsync();
        ViewData["LopSelect"] = lops.Select(l => new SelectListItem
        {
            Value = l.Id.ToString(),
            Text = $"{l.MaLop} – {l.TenLop} ({l.Khoa?.TenKhoa})",
            Selected = l.Id == selectedId
        }).ToList();
    }

    #region CALL API

    public async Task<IActionResult> GetAllSinhVien()
    {
        var sinhViens = await _sinhVienService.GetAllSinhVienAsync();

        // Chỉ trả các cột cần hiển thị – không trả ApplicationUser (có PasswordHash, SecurityStamp...)
        var data = sinhViens.Select(s => new
        {
            s.Id,
            s.MaSinhVien,
            s.HoTen,
            NgaySinh = s.NgaySinh.ToString("dd/MM/yyyy"),
            NgaySinhSort = s.NgaySinh.ToString("yyyy-MM-dd"),
            GioiTinh = s.GioiTinh ? "Nam" : "Nữ",
            MaLop = s.Lop?.MaLop ?? "",
            LopId = s.LopId,
            s.KhoaId,
            TenKhoa = s.Khoa?.TenKhoa ?? "",
            s.Email,
            s.SoDienThoai
        });
        return Json(new { data });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ sinhvien.js
    public async Task<IActionResult> DeleteSinhVien(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        var (success, message) = await _sinhVienService.DeleteSinhVienAsync(id.Value);
        return Json(new { success, message });
    }

    #endregion
}
