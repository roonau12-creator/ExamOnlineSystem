using ClosedXML.Excel;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

// Danh sách đáp án + thêm nhanh đáp án vào câu hỏi trắc nghiệm có sẵn.
// Sửa/xóa đáp án thực hiện trong form Sửa câu hỏi.
[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class DapAnController : Controller
{
    private readonly ICauHoiService _cauHoiService;
    private readonly IDanhMucCauHoiService _danhMucCauHoiService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DapAnController(ICauHoiService cauHoiService, IDanhMucCauHoiService danhMucCauHoiService,
        UserManager<ApplicationUser> userManager)
    {
        _cauHoiService = cauHoiService;
        _danhMucCauHoiService = danhMucCauHoiService;
        _userManager = userManager;
    }

    // GET: /Admin/DapAn
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Admin/DapAn/Create?cauHoiId=5 (cauHoiId không bắt buộc: chọn sẵn câu hỏi)
    public async Task<IActionResult> Create(int? cauHoiId)
    {
        var vm = new DapAnFormVM();
        if (cauHoiId != null)
        {
            var cauHoi = await _cauHoiService.GetCauHoiByIdAsync(cauHoiId.Value);
            if (cauHoi != null && cauHoi.LoaiCauHoi == 1 && await CoQuyenSuaAsync(cauHoi))
            {
                vm.CauHoiId = cauHoi.Id;
                vm.MonHocId = cauHoi.DanhMucCauHoi?.MonHocId;
            }
        }
        await LoadDropdowns(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DapAnFormVM vm, bool themTiep = false)
    {
        CauHoi? cauHoi = null;
        if (vm.CauHoiId > 0)
        {
            cauHoi = await _cauHoiService.GetCauHoiByIdAsync(vm.CauHoiId);
            if (cauHoi == null)
            {
                ModelState.AddModelError(nameof(vm.CauHoiId), "Câu hỏi không tồn tại");
            }
            else if (cauHoi.LoaiCauHoi != 1)
            {
                ModelState.AddModelError(nameof(vm.CauHoiId), "Chỉ câu hỏi trắc nghiệm mới có đáp án");
            }
            else if (!await CoQuyenSuaAsync(cauHoi))
            {
                ModelState.AddModelError(nameof(vm.CauHoiId), "Bạn chỉ được thêm đáp án vào câu hỏi do mình tạo");
            }
            else if (await ThongBaoKhoaAsync(cauHoi.Id) is { } khoa)
            {
                ModelState.AddModelError(nameof(vm.CauHoiId), khoa);
            }
            else
            {
                var dapAnCu = await _cauHoiService.GetDapAnsAsync(cauHoi.Id);
                if (dapAnCu.Count >= DapAnFormVM.SoDapAnToiDa)
                {
                    ModelState.AddModelError(nameof(vm.CauHoiId),
                        $"Câu hỏi đã có đủ {DapAnFormVM.SoDapAnToiDa} đáp án, không thể thêm");
                }
                else if (!string.IsNullOrWhiteSpace(vm.NoiDung) &&
                         dapAnCu.Any(d => d.NoiDung.Trim().Equals(vm.NoiDung.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError(nameof(vm.NoiDung), "Câu hỏi đã có đáp án với nội dung này");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            vm.MonHocId ??= cauHoi?.DanhMucCauHoi?.MonHocId;
            await LoadDropdowns(vm);
            return View(vm);
        }

        var dapAn = await _cauHoiService.AddDapAnAsync(new DapAn
        {
            CauHoiId = vm.CauHoiId,
            NoiDung = vm.NoiDung.Trim(),
            DapAnDung = vm.DapAnDung
        });
        var nhan = dapAn.ThuTu <= 26 ? ((char)('A' + dapAn.ThuTu - 1)).ToString() : dapAn.ThuTu.ToString();
        TempData["success"] = $"Đã thêm đáp án {nhan} vào câu hỏi #{vm.CauHoiId}";

        // "Lưu & thêm đáp án khác": mở lại form, giữ câu hỏi vừa chọn; không thì xem câu hỏi với đầy đủ đáp án
        return themTiep
            ? RedirectToAction("Create", new { cauHoiId = vm.CauHoiId })
            : RedirectToAction("Details", "CauHoi", new { id = vm.CauHoiId });
    }

    // GET: /Admin/DapAn/Edit/5?returnUrl=/Admin/DapAn
    public async Task<IActionResult> Edit(int? id, string? returnUrl)
    {
        if (id == null)
        {
            return NotFound();
        }

        var dapAn = await _cauHoiService.GetDapAnByIdAsync(id.Value);
        if (dapAn == null)
        {
            return NotFound();
        }
        var cauHoi = await _cauHoiService.GetCauHoiByIdAsync(dapAn.CauHoiId);
        if (cauHoi == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(cauHoi))
        {
            TempData["error"] = "Bạn chỉ được sửa đáp án của câu hỏi do mình tạo";
            return RedirectToAction("Index");
        }
        if (await ThongBaoKhoaAsync(cauHoi.Id) is { } khoaSua)
        {
            TempData["error"] = khoaSua;
            return RedirectToAction("Details", "CauHoi", new { id = cauHoi.Id });
        }

        var vm = new DapAnFormVM
        {
            Id = dapAn.Id,
            CauHoiId = dapAn.CauHoiId,
            NoiDung = dapAn.NoiDung,
            DapAnDung = dapAn.DapAnDung,
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
        };
        await LoadEditInfo(cauHoi, dapAn.Id);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DapAnFormVM vm)
    {
        if (id != vm.Id)
        {
            return NotFound();
        }

        // Câu hỏi lấy từ đáp án trong DB, không tin CauHoiId gửi lên (không cho chuyển đáp án sang câu khác)
        var dapAn = await _cauHoiService.GetDapAnByIdAsync(id);
        if (dapAn == null)
        {
            return NotFound();
        }
        var cauHoi = await _cauHoiService.GetCauHoiByIdAsync(dapAn.CauHoiId);
        if (cauHoi == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(cauHoi))
        {
            TempData["error"] = "Bạn chỉ được sửa đáp án của câu hỏi do mình tạo";
            return RedirectToAction("Index");
        }
        if (await ThongBaoKhoaAsync(cauHoi.Id) is { } khoaSua)
        {
            TempData["error"] = khoaSua;
            return RedirectToAction("Details", "CauHoi", new { id = cauHoi.Id });
        }
        vm.CauHoiId = cauHoi.Id;
        ModelState.Remove(nameof(vm.CauHoiId));

        var cacDapAnKhac = (await _cauHoiService.GetDapAnsAsync(cauHoi.Id)).Where(d => d.Id != id).ToList();
        if (!string.IsNullOrWhiteSpace(vm.NoiDung) &&
            cacDapAnKhac.Any(d => d.NoiDung.Trim().Equals(vm.NoiDung.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(vm.NoiDung), "Câu hỏi đã có đáp án khác với nội dung này");
        }
        if (!vm.DapAnDung && !cacDapAnKhac.Any(d => d.DapAnDung))
        {
            ModelState.AddModelError(nameof(vm.DapAnDung),
                "Đây là đáp án đúng duy nhất của câu hỏi. Hãy đánh dấu đáp án đúng khác trước khi bỏ đánh dấu đáp án này.");
        }

        if (!ModelState.IsValid)
        {
            await LoadEditInfo(cauHoi, id);
            return View(vm);
        }

        await _cauHoiService.UpdateDapAnAsync(id, vm.NoiDung.Trim(), vm.DapAnDung);
        TempData["success"] = "Cập nhật đáp án thành công";

        return Url.IsLocalUrl(vm.ReturnUrl)
            ? LocalRedirect(vm.ReturnUrl!)
            : RedirectToAction("Details", "CauHoi", new { id = cauHoi.Id });
    }

    #region  NHẬP EXCEL

    public IActionResult Import()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
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

        // Đọc file: mỗi dòng = 1 đáp án
        var errors = new List<string>();
        var dong = new List<(int Dong, int CauHoiId, string NoiDung, bool Dung)>();
        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

            // Xác định cột theo tiêu đề ở dòng 1; không nhận ra thì dùng thứ tự A-B-C của file mẫu
            int colCauHoi = 1, colNoiDung = 2, colDung = 3;
            var lastCol = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (var c = 1; c <= lastCol; c++)
            {
                var h = ChuanHoa(sheet.Cell(1, c).GetFormattedString());
                if (h.Contains("câu hỏi")) colCauHoi = c;
                else if (h.Contains("đúng")) colDung = c;
                else if (h.Contains("nội dung")) colNoiDung = c;
            }

            for (var r = 2; r <= lastRow; r++)
            {
                var maText = sheet.Cell(r, colCauHoi).GetFormattedString().Trim().TrimStart('#');
                var noiDung = sheet.Cell(r, colNoiDung).GetFormattedString().Trim();
                var dungText = sheet.Cell(r, colDung).GetFormattedString().Trim();
                if (maText == "" && noiDung == "" && dungText == "")
                {
                    continue; // bỏ qua dòng trống
                }

                var ok = true;
                if (!int.TryParse(maText, out var cauHoiId) || cauHoiId <= 0)
                {
                    errors.Add($"Dòng {r}: mã câu hỏi \"{maText}\" không hợp lệ (vd: 9 hoặc #9)"); ok = false;
                }
                if (noiDung == "")
                {
                    errors.Add($"Dòng {r}: thiếu nội dung đáp án"); ok = false;
                }
                var dung = DocDungSai(dungText);
                if (dung == null)
                {
                    errors.Add($"Dòng {r}: cột đáp án đúng \"{dungText}\" không hợp lệ (để trống/Sai hoặc x/Đúng)"); ok = false;
                }
                if (ok)
                {
                    dong.Add((r, cauHoiId, noiDung, dung!.Value));
                }
            }
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Không đọc được file Excel. Vui lòng kiểm tra lại định dạng file.");
            return View();
        }

        // Kiểm tra câu hỏi: tồn tại, là trắc nghiệm, người dùng có quyền
        var duocPhep = (await CauHoiDuocPhepAsync()).Select(c => c.Id).ToHashSet();
        foreach (var id in dong.Select(d => d.CauHoiId).Distinct().Where(id => !duocPhep.Contains(id)))
        {
            var cauHoi = await _cauHoiService.GetCauHoiByIdAsync(id);
            var lyDo = cauHoi == null ? "không tồn tại"
                     : cauHoi.LoaiCauHoi != 1 ? "là câu tự luận, không có đáp án"
                     : "không phải câu hỏi do bạn tạo";
            var cacDong = string.Join(", ", dong.Where(d => d.CauHoiId == id).Select(d => d.Dong));
            errors.Add($"Dòng {cacDong}: câu hỏi #{id} {lyDo}");
        }
        dong = dong.Where(d => duocPhep.Contains(d.CauHoiId)).ToList();

        // Câu hỏi trong đề đã phát hành bị khóa
        var biKhoa = await _cauHoiService.GetDePhatHanhChuaCauAsync(dong.Select(d => d.CauHoiId));
        foreach (var (cauId, maDe) in biKhoa)
        {
            var cacDong = string.Join(", ", dong.Where(d => d.CauHoiId == cauId).Select(d => d.Dong));
            errors.Add($"Dòng {cacDong}: câu hỏi #{cauId} đang nằm trong đề đã phát hành ({string.Join(", ", maDe)}) nên bị khóa");
        }
        dong = dong.Where(d => !biKhoa.ContainsKey(d.CauHoiId)).ToList();

        // Kiểm tra theo từng câu hỏi: trùng, số lượng, đáp án đúng
        var dapAnCu = await _cauHoiService.GetDapAnsTheoCauHoiAsync(dong.Select(d => d.CauHoiId));
        var themMoi = new List<DapAn>();
        var soBoQua = 0;
        foreach (var nhom in dong.GroupBy(d => d.CauHoiId))
        {
            var cu = dapAnCu[nhom.Key];
            var noiDungCu = cu.Select(d => ChuanHoa(d.NoiDung)).ToHashSet();
            var trongFile = new Dictionary<string, int>();
            var moi = new List<DapAn>();

            foreach (var d in nhom)
            {
                var khoa = ChuanHoa(d.NoiDung);
                if (trongFile.TryGetValue(khoa, out var dongTruoc))
                {
                    errors.Add($"Dòng {d.Dong}: trùng đáp án ở dòng {dongTruoc} (câu hỏi #{nhom.Key})");
                    continue;
                }
                trongFile[khoa] = d.Dong;
                if (noiDungCu.Contains(khoa))
                {
                    soBoQua++; // câu hỏi đã có đáp án này -> bỏ qua
                    continue;
                }
                moi.Add(new DapAn { CauHoiId = nhom.Key, NoiDung = d.NoiDung, DapAnDung = d.Dung });
            }

            var tong = cu.Count + moi.Count;
            if (tong > DapAnFormVM.SoDapAnToiDa)
            {
                errors.Add($"Câu hỏi #{nhom.Key}: đang có {cu.Count} đáp án, thêm {moi.Count} sẽ vượt quá {DapAnFormVM.SoDapAnToiDa} đáp án");
            }
            else if (tong < 2)
            {
                errors.Add($"Câu hỏi #{nhom.Key}: phải có ít nhất 2 đáp án sau khi nhập");
            }
            else if (!cu.Any(x => x.DapAnDung) && !moi.Any(x => x.DapAnDung))
            {
                errors.Add($"Câu hỏi #{nhom.Key}: chưa có đáp án đúng nào, hãy đánh dấu ít nhất 1 đáp án đúng");
            }
            themMoi.AddRange(moi);
        }

        if (errors.Count > 0)
        {
            ViewBag.Errors = errors;
            return View();
        }
        if (themMoi.Count == 0)
        {
            ModelState.AddModelError(string.Empty, soBoQua > 0
                ? $"Tất cả {soBoQua} đáp án trong file đã có trong hệ thống, không có đáp án nào mới"
                : "File không có đáp án nào");
            return View();
        }

        var added = await _cauHoiService.ImportDapAnAsync(themMoi);
        var soCau = themMoi.Select(d => d.CauHoiId).Distinct().Count();
        TempData["success"] = $"Nhập Excel thành công: thêm {added} đáp án cho {soCau} câu hỏi" +
                              (soBoQua > 0 ? $", bỏ qua {soBoQua} đáp án đã có" : "");
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> DownloadTemplate()
    {
        var cauHois = await CauHoiDuocPhepAsync();
        var monHocs = (await _danhMucCauHoiService.GetAllMonHocAsync()).ToDictionary(m => m.Id, m => m.TenMonHoc);
        var dapAnCu = await _cauHoiService.GetDapAnsTheoCauHoiAsync(cauHois.Select(c => c.Id));

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("DapAn");
        sheet.Cell(1, 1).Value = "Mã câu hỏi";
        sheet.Cell(1, 2).Value = "Nội dung đáp án";
        sheet.Cell(1, 3).Value = "Đáp án đúng";
        var maMau = cauHois.FirstOrDefault()?.Id ?? 1;
        sheet.Cell(2, 1).Value = maMau;
        sheet.Cell(2, 2).Value = "Nội dung đáp án mẫu (đúng)";
        sheet.Cell(2, 3).Value = "x";
        sheet.Cell(3, 1).Value = maMau;
        sheet.Cell(3, 2).Value = "Nội dung đáp án mẫu (sai, để trống cột Đáp án đúng)";
        sheet.Range("C2:C2000").CreateDataValidation().List("\"x,Đúng,Sai\"");
        StyleHeader(sheet.Range(1, 1, 1, 3));
        sheet.Columns().AdjustToContents();
        sheet.Column(2).Width = 60;

        // Sheet tham khảo: các câu trắc nghiệm được phép thêm đáp án, kèm đáp án hiện có
        var ref_ = workbook.Worksheets.Add("DanhSachCauHoi");
        string[] tieuDe = { "Mã câu hỏi", "Môn học", "Nội dung câu hỏi", "Số đáp án", "Đáp án hiện có" };
        for (var i = 0; i < tieuDe.Length; i++)
        {
            ref_.Cell(1, i + 1).Value = tieuDe[i];
        }
        for (var i = 0; i < cauHois.Count; i++)
        {
            var c = cauHois[i];
            var cu = dapAnCu[c.Id];
            ref_.Cell(i + 2, 1).Value = c.Id;
            ref_.Cell(i + 2, 2).Value = monHocs.GetValueOrDefault(c.MonHocId, "");
            ref_.Cell(i + 2, 3).Value = c.NoiDung;
            ref_.Cell(i + 2, 4).Value = cu.Count;
            ref_.Cell(i + 2, 5).Value = string.Join(" | ",
                cu.Select((d, j) => $"{(char)('A' + j)}. {d.NoiDung}{(d.DapAnDung ? " ✓" : "")}"));
        }
        StyleHeader(ref_.Range(1, 1, 1, tieuDe.Length));
        ref_.Columns().AdjustToContents();
        ref_.Column(3).Width = 60;
        ref_.Column(5).Width = 80;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapDapAn.xlsx");
    }

    private static void StyleHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
    }

    // Chuẩn hóa chuỗi để so sánh: Unicode NFC, chữ thường, gộp khoảng trắng
    private static string ChuanHoa(string s) =>
        string.Join(' ', s.Normalize().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static bool? DocDungSai(string s) => ChuanHoa(s) switch
    {
        "" or "sai" or "0" or "false" or "không" => false,
        "x" or "đúng" or "1" or "true" or "có" => true,
        _ => null
    };

    // Câu trắc nghiệm người dùng được thêm đáp án: Admin = tất cả, giảng viên = câu do mình tạo
    private async Task<List<CauHoiLuaChonVM>> CauHoiDuocPhepAsync()
    {
        int? chiCuaGiaoVien = null;
        if (!User.IsInRole(SD.Role_Admin))
        {
            chiCuaGiaoVien = (await GiaoVienHienTaiAsync())?.Id ?? -1;
        }
        return await _cauHoiService.GetCauHoiTracNghiemAsync(chiCuaGiaoVien);
    }

    #endregion

    // Thông tin hiển thị trên form Sửa đáp án: câu hỏi, các đáp án của câu, số đề thi đang dùng
    private async Task LoadEditInfo(CauHoi cauHoi, int dapAnId)
    {
        var dapAns = await _cauHoiService.GetDapAnsAsync(cauHoi.Id);
        ViewData["CauHoi"] = cauHoi;
        ViewData["DapAns"] = dapAns;
        ViewData["DapAnDangSua"] = dapAnId;
        ViewData["SoDeThi"] = await _cauHoiService.CountDeThiDangDungAsync(cauHoi.Id);
    }

    // Câu hỏi nằm trong đề đã phát hành thì bị khóa (không sửa câu hỏi, không thêm/sửa/xóa đáp án).
    // Trả về thông báo lỗi, hoặc null nếu không bị khóa.
    private async Task<string?> ThongBaoKhoaAsync(int cauHoiId)
    {
        var khoa = await _cauHoiService.GetDePhatHanhChuaCauAsync(new[] { cauHoiId });
        return khoa.TryGetValue(cauHoiId, out var maDe)
            ? $"Câu hỏi #{cauHoiId} đang nằm trong đề đã phát hành ({string.Join(", ", maDe)}) nên bị khóa chỉnh sửa. " +
              "Hãy hủy phát hành các đề này trước."
            : null;
    }

    // Admin thêm/sửa được đáp án của mọi câu hỏi; giảng viên chỉ với câu hỏi do mình tạo
    private async Task<bool> CoQuyenSuaAsync(CauHoi cauHoi)
    {
        if (User.IsInRole(SD.Role_Admin))
        {
            return true;
        }
        var giaoVien = await GiaoVienHienTaiAsync();
        return giaoVien != null && giaoVien.Id == cauHoi.GiaoVienId;
    }

    private async Task<GiaoVien?> GiaoVienHienTaiAsync()
    {
        var userId = _userManager.GetUserId(User);
        return userId == null ? null : await _cauHoiService.GetGiaoVienByUserIdAsync(userId);
    }

    // Dropdown môn học + câu hỏi trắc nghiệm mà người dùng được phép thêm đáp án
    private async Task LoadDropdowns(DapAnFormVM vm)
    {
        var monHocs = await _danhMucCauHoiService.GetAllMonHocAsync();
        ViewData["MonHocList"] = new SelectList(monHocs, nameof(MonHoc.Id), nameof(MonHoc.TenMonHoc), vm.MonHocId);

        int? chiCuaGiaoVien = null;
        if (!User.IsInRole(SD.Role_Admin))
        {
            chiCuaGiaoVien = (await GiaoVienHienTaiAsync())?.Id ?? -1; // không phải giảng viên => không có câu nào
        }
        ViewData["CauHoiList"] = await _cauHoiService.GetCauHoiTracNghiemAsync(chiCuaGiaoVien);
    }

    #region  CALL API

    public async Task<IActionResult> GetAllDapAn()
    {
        var dapAns = await _cauHoiService.GetAllDapAnAsync();

        // Đường dẫn đầy đủ của danh mục (danh mục nhiều cấp)
        var duongDan = (await _danhMucCauHoiService.GetDanhMucTreeAsync())
            .ToDictionary(n => n.DanhMuc.Id,
                          n => string.Join(" › ", n.ToTien.Select(t => t.TenDanhMuc).Append(n.DanhMuc.TenDanhMuc)));

        var data = dapAns.Select(d => new
        {
            d.Id,
            d.CauHoiId,
            d.NoiDungCauHoi,
            DanhMuc = duongDan.GetValueOrDefault(d.DanhMucCauHoiId, ""),
            d.TenMonHoc,
            // Nhãn A, B, C... theo thứ tự đáp án trong câu hỏi
            Nhan = d.ThuTu >= 1 && d.ThuTu <= 26 ? ((char)('A' + d.ThuTu - 1)).ToString() : d.ThuTu.ToString(),
            d.ThuTu,
            d.NoiDung,
            d.DapAnDung,
            TenMucDo = CauHoiLabel.TenMucDo(d.MucDo),
            d.TenGiaoVien
        });
        return Json(new { data });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken"
    public async Task<IActionResult> DeleteDapAn(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        var dapAn = await _cauHoiService.GetDapAnByIdAsync(id.Value);
        if (dapAn == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đáp án cần xóa" });
        }
        var cauHoi = await _cauHoiService.GetCauHoiByIdAsync(dapAn.CauHoiId);
        if (cauHoi == null || !await CoQuyenSuaAsync(cauHoi))
        {
            return Json(new { success = false, message = "Bạn chỉ được xóa đáp án của câu hỏi do mình tạo" });
        }
        if (await ThongBaoKhoaAsync(cauHoi.Id) is { } khoaXoa)
        {
            return Json(new { success = false, message = khoaXoa });
        }

        // Câu trắc nghiệm phải còn ít nhất 2 đáp án và ít nhất 1 đáp án đúng
        var cacDapAn = await _cauHoiService.GetDapAnsAsync(cauHoi.Id);
        if (cauHoi.LoaiCauHoi == 1 && cacDapAn.Count <= 2)
        {
            return Json(new { success = false, message = "Câu trắc nghiệm phải có ít nhất 2 đáp án, không thể xóa thêm" });
        }
        if (dapAn.DapAnDung && !cacDapAn.Any(d => d.Id != dapAn.Id && d.DapAnDung))
        {
            return Json(new
            {
                success = false,
                message = "Đây là đáp án đúng duy nhất của câu hỏi. Hãy đánh dấu đáp án đúng khác trước khi xóa."
            });
        }

        await _cauHoiService.DeleteDapAnAsync(dapAn.Id);
        return Json(new { success = true, message = "Xóa đáp án thành công" });
    }

    // Các đáp án hiện có của một câu hỏi (form Thêm đáp án hiển thị để tránh nhập trùng)
    public async Task<IActionResult> GetDapAnByCauHoi(int cauHoiId)
    {
        var dapAns = await _cauHoiService.GetDapAnsAsync(cauHoiId);
        var data = dapAns.Select((d, i) => new
        {
            Nhan = i < 26 ? ((char)('A' + i)).ToString() : (i + 1).ToString(),
            d.NoiDung,
            d.DapAnDung
        });
        return Json(new { data });
    }

    #endregion
}
