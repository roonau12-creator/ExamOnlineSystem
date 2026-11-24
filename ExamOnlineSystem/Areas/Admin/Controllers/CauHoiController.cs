using ClosedXML.Excel;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Services;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class CauHoiController : Controller
{
    private const int SoDapAnMacDinh = 4;

    private readonly ICauHoiService _cauHoiService;
    private readonly IDanhMucCauHoiService _danhMucCauHoiService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HinhAnhCauHoiStorage _hinhAnh;

    public CauHoiController(ICauHoiService cauHoiService, IDanhMucCauHoiService danhMucCauHoiService,
        UserManager<ApplicationUser> userManager, HinhAnhCauHoiStorage hinhAnh)
    {
        _cauHoiService = cauHoiService;
        _danhMucCauHoiService = danhMucCauHoiService;
        _userManager = userManager;
        _hinhAnh = hinhAnh;
    }

    // GET: /Admin/CauHoi/HinhAnh/5 — trả ảnh minh họa của câu hỏi (chỉ người đã đăng nhập có quyền mới xem được)
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> HinhAnh(int id)
    {
        var cauHoi = await _cauHoiService.GetCauHoiByIdAsync(id);
        var duongDan = _hinhAnh.DuongDan(cauHoi?.HinhAnh);
        if (duongDan == null || !System.IO.File.Exists(duongDan))
        {
            return NotFound();
        }
        return PhysicalFile(duongDan, HinhAnhCauHoiStorage.ContentType(cauHoi!.HinhAnh!));
    }

    // GET: /Admin/CauHoi
    public async Task<IActionResult> Index()
    {
        ViewData["DanhMucList"] = await _danhMucCauHoiService.GetDanhMucTreeAsync(); // bộ lọc danh mục (dạng cây)
        return View();
    }

    // GET: /Admin/CauHoi/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cauHoi = await _cauHoiService.GetCauHoiByIdAsync(id.Value);
        if (cauHoi == null)
        {
            return NotFound();
        }

        // Lấy đường dẫn danh mục nhiều cấp và môn học từ cây danh mục
        var node = (await _danhMucCauHoiService.GetDanhMucTreeAsync())
            .FirstOrDefault(n => n.DanhMuc.Id == cauHoi.DanhMucCauHoiId);

        var vm = new CauHoiDetailVM
        {
            CauHoi = cauHoi,
            TenMonHoc = node?.DanhMuc.MonHoc?.TenMonHoc ?? "",
            DuongDanDanhMuc = node == null
                ? new List<DanhMucCauHoi>()
                : node.ToTien.Append(node.DanhMuc).ToList(),
            DapAns = await _cauHoiService.GetDapAnsAsync(cauHoi.Id),
            CauHoiDeThis = await _cauHoiService.GetCauHoiDeThisAsync(cauHoi.Id),
            CoQuyenSua = await CoQuyenSuaAsync(cauHoi)
        };
        return View(vm);
    }

    // GET: /Admin/CauHoi/Create?danhMucId=5 (danhMucId không bắt buộc: chọn sẵn danh mục)
    public async Task<IActionResult> Create(int? danhMucId)
    {
        var vm = new CauHoiFormVM();
        if (danhMucId != null)
        {
            var danhMuc = await _danhMucCauHoiService.GetDanhMucCauHoiByIdAsync(danhMucId.Value);
            if (danhMuc != null)
            {
                vm.DanhMucCauHoiId = danhMuc.Id;
                vm.MonHocId = danhMuc.MonHocId;
            }
        }
        for (var i = 0; i < SoDapAnMacDinh; i++)
        {
            vm.DapAns.Add(new DapAnInputVM());
        }

        await LoadDropdowns(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CauHoiFormVM vm, bool themTiep = false)
    {
        var danhMuc = await ValidateDanhMucAsync(vm);
        // Giảng viên: tài khoản giảng viên thì dùng chính họ, ngược lại (Admin) phải chọn
        var giaoVienId = await XacDinhGiaoVienAsync(vm.GiaoVienId);
        var dapAns = ValidateDapAns(vm);
        ValidateHinhAnh(vm);

        if (!ModelState.IsValid || giaoVienId == null)
        {
            await ChuanBiHienLaiForm(vm, danhMuc);
            return View(vm);
        }

        var cauHoi = TaoCauHoiTuForm(vm);
        cauHoi.GiaoVienId = giaoVienId.Value;
        if (vm.HinhAnhFile != null)
        {
            cauHoi.HinhAnh = await _hinhAnh.LuuAsync(vm.HinhAnhFile);
        }
        try
        {
            await _cauHoiService.CreateCauHoiAsync(cauHoi, dapAns);
        }
        catch
        {
            _hinhAnh.Xoa(cauHoi.HinhAnh); // không để lại file mồ côi khi lưu DB lỗi
            throw;
        }
        TempData["success"] = "Thêm câu hỏi thành công";

        // "Lưu & thêm câu khác": mở lại form, giữ danh mục vừa chọn
        return themTiep
            ? RedirectToAction("Create", new { danhMucId = vm.DanhMucCauHoiId })
            : RedirectToAction("Index");
    }

    // GET: /Admin/CauHoi/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cauHoi = await _cauHoiService.GetCauHoiByIdAsync(id.Value);
        if (cauHoi == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(cauHoi))
        {
            TempData["error"] = "Bạn chỉ được sửa câu hỏi do mình tạo";
            return RedirectToAction("Index");
        }
        if (await ThongBaoKhoaAsync(cauHoi.Id) is { } khoaGet)
        {
            TempData["error"] = khoaGet;
            return RedirectToAction("Details", new { id = cauHoi.Id });
        }

        var vm = new CauHoiFormVM
        {
            Id = cauHoi.Id,
            MonHocId = cauHoi.DanhMucCauHoi?.MonHocId,
            DanhMucCauHoiId = cauHoi.DanhMucCauHoiId,
            GiaoVienId = cauHoi.GiaoVienId,
            TenGiaoVien = cauHoi.GiaoVien == null ? null : $"{cauHoi.GiaoVien.HoTen} ({cauHoi.GiaoVien.MaGiaoVien})",
            NoiDung = cauHoi.NoiDung,
            LoaiCauHoi = cauHoi.LoaiCauHoi,
            MucDo = cauHoi.MucDo,
            DiemMacDinh = cauHoi.DiemMacDinh,
            GiaiThich = cauHoi.GiaiThich,
            HinhAnh = cauHoi.HinhAnh,
            DapAns = (await _cauHoiService.GetDapAnsAsync(cauHoi.Id))
                .Select(d => new DapAnInputVM { Id = d.Id, NoiDung = d.NoiDung, DapAnDung = d.DapAnDung })
                .ToList()
        };
        // Câu tự luận chưa có đáp án: chuẩn bị sẵn dòng trống phòng khi đổi sang trắc nghiệm
        while (vm.DapAns.Count < 2)
        {
            vm.DapAns.Add(new DapAnInputVM());
        }

        await ChuanBiHienLaiForm(vm, null);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CauHoiFormVM vm)
    {
        if (id != vm.Id)
        {
            return NotFound();
        }

        var cu = await _cauHoiService.GetCauHoiByIdAsync(id);
        if (cu == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(cu))
        {
            TempData["error"] = "Bạn chỉ được sửa câu hỏi do mình tạo";
            return RedirectToAction("Index");
        }
        if (await ThongBaoKhoaAsync(cu.Id) is { } khoaPost)
        {
            TempData["error"] = khoaPost;
            return RedirectToAction("Details", new { id = cu.Id });
        }

        var danhMuc = await ValidateDanhMucAsync(vm);
        var dapAns = ValidateDapAns(vm);
        ValidateHinhAnh(vm);

        // Ảnh hiện tại lấy từ DB, không tin giá trị gửi lên từ form
        vm.HinhAnh = cu.HinhAnh;

        if (!ModelState.IsValid)
        {
            vm.TenGiaoVien = cu.GiaoVien == null ? null : $"{cu.GiaoVien.HoTen} ({cu.GiaoVien.MaGiaoVien})";
            await ChuanBiHienLaiForm(vm, danhMuc);
            return View(vm);
        }

        var cauHoi = TaoCauHoiTuForm(vm);
        cauHoi.Id = id;
        // Ảnh mới thay ảnh cũ; tick "Xóa ảnh" thì bỏ ảnh; không thì giữ nguyên
        cauHoi.HinhAnh = vm.HinhAnhFile != null ? await _hinhAnh.LuuAsync(vm.HinhAnhFile)
                       : vm.XoaHinhAnh ? null
                       : cu.HinhAnh;
        try
        {
            await _cauHoiService.UpdateCauHoiAsync(cauHoi, dapAns);
        }
        catch
        {
            if (cauHoi.HinhAnh != cu.HinhAnh)
            {
                _hinhAnh.Xoa(cauHoi.HinhAnh); // DB lỗi: bỏ file mới vừa lưu, giữ ảnh cũ
            }
            throw;
        }
        if (cauHoi.HinhAnh != cu.HinhAnh)
        {
            _hinhAnh.Xoa(cu.HinhAnh); // đã lưu DB thành công mới xóa file ảnh cũ
        }
        TempData["success"] = "Cập nhật câu hỏi thành công";
        return RedirectToAction("Index");
    }

    #region  NHẬP EXCEL

    private const int SoCotDapAnToiDa = 10; // Đáp án A..J

    public async Task<IActionResult> Import()
    {
        await LoadGiaoVienChoImport(null);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file, int? giaoVienId)
    {
        // Giảng viên: tài khoản giảng viên thì dùng chính họ, ngược lại (Admin) phải chọn trên form
        var gvId = await XacDinhGiaoVienAsync(giaoVienId);

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn file Excel");
        }
        else if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Chỉ hỗ trợ file .xlsx");
        }
        if (!ModelState.IsValid || gvId == null)
        {
            await LoadGiaoVienChoImport(giaoVienId);
            return View();
        }

        // Tra danh mục theo (môn học, đường dẫn đầy đủ) — không phân biệt hoa thường
        var monHocs = (await _danhMucCauHoiService.GetAllMonHocAsync()).ToList();
        var danhMucTheoDuongDan = (await _danhMucCauHoiService.GetDanhMucTreeAsync())
            .GroupBy(n => KhoaDanhMuc(n.DanhMuc.MonHocId,
                n.ToTien.Select(t => t.TenDanhMuc).Append(n.DanhMuc.TenDanhMuc)))
            .ToDictionary(g => g.Key, g => g.First().DanhMuc.Id);

        var daCoTrongDb = await _cauHoiService.GetCauHoiKeysAsync();
        var daCoTrongFile = new Dictionary<string, int>(); // khóa -> dòng đầu tiên
        var errors = new List<string>();
        var rows = new List<CauHoiImportRow>();
        var soTrung = 0;

        try
        {
            using var stream = file!.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

            // Xác định cột theo tiêu đề ở dòng 1; không nhận ra thì dùng thứ tự của file mẫu
            int colMon = 1, colDm = 2, colNd = 3, colLoai = 4, colMucDo = 5, colDiem = 6, colDung = 11, colGt = 12;
            var colDapAn = Enumerable.Range(7, 4).ToList(); // G..J = Đáp án A..D
            var lastCol = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            var dapAnTheoTieuDe = new SortedDictionary<char, int>();
            for (var c = 1; c <= lastCol; c++)
            {
                var h = ChuanHoa(sheet.Cell(1, c).GetFormattedString());
                var m = System.Text.RegularExpressions.Regex.Match(h, @"^đáp án ([a-j])$");
                if (h.Contains("đáp án đúng")) colDung = c;
                else if (m.Success) dapAnTheoTieuDe[char.ToUpperInvariant(m.Groups[1].Value[0])] = c;
                else if (h.Contains("môn")) colMon = c;
                else if (h.Contains("danh mục")) colDm = c;
                else if (h.Contains("nội dung")) colNd = c;
                else if (h.Contains("loại")) colLoai = c;
                else if (h.Contains("mức độ")) colMucDo = c;
                else if (h.Contains("điểm")) colDiem = c;
                else if (h.Contains("giải thích")) colGt = c;
            }
            if (dapAnTheoTieuDe.Count > 0)
            {
                colDapAn = dapAnTheoTieuDe.Values.ToList();
            }

            for (var r = 2; r <= lastRow; r++)
            {
                string O(int c) => sheet.Cell(r, c).GetFormattedString().Trim();

                var maMon = O(colMon);
                var dm = O(colDm);
                var noiDung = O(colNd);
                var dapAnText = colDapAn.Select(O).ToList();
                if (maMon == "" && dm == "" && noiDung == "" && dapAnText.All(x => x == ""))
                {
                    continue; // bỏ qua dòng trống
                }

                var loi = new List<string>();

                // Môn học + danh mục
                int? danhMucId = null;
                var monHoc = monHocs.FirstOrDefault(x => x.MaMonHoc.Equals(maMon, StringComparison.OrdinalIgnoreCase));
                if (maMon == "") loi.Add("thiếu mã môn học");
                else if (monHoc == null) loi.Add($"không tìm thấy môn học có mã \"{maMon}\"");
                if (dm == "") loi.Add("thiếu danh mục");
                else if (monHoc != null)
                {
                    var duongDan = dm.Split(new[] { '>', '›' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (danhMucTheoDuongDan.TryGetValue(KhoaDanhMuc(monHoc.Id, duongDan), out var id)) danhMucId = id;
                    else loi.Add($"không tìm thấy danh mục \"{dm}\" trong môn {monHoc.MaMonHoc}");
                }

                if (noiDung == "") loi.Add("thiếu nội dung câu hỏi");

                var loai = DocLoai(O(colLoai));
                if (loai == null) loi.Add($"loại câu hỏi \"{O(colLoai)}\" không hợp lệ (Trắc nghiệm / Tự luận)");

                var mucDo = DocMucDo(O(colMucDo));
                if (mucDo == null) loi.Add($"mức độ \"{O(colMucDo)}\" không hợp lệ (Dễ / Trung bình / Khó / Rất khó)");

                var diemText = O(colDiem);
                double diem = 1; // để trống -> 1 điểm
                if (diemText != "" && (!double.TryParse(diemText.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out diem) || diem <= 0 || diem > 100))
                {
                    loi.Add($"điểm \"{diemText}\" không hợp lệ (số > 0 và ≤ 100)");
                }

                // Đáp án (chỉ câu trắc nghiệm)
                var dapAns = new List<DapAn>();
                if (loai == 1)
                {
                    var dungText = O(colDung).ToUpperInvariant();
                    var chuDung = dungText.Split(new[] { ',', ';', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    var chiSoDung = new HashSet<int>();
                    foreach (var chu in chuDung)
                    {
                        if (chu.Length == 1 && chu[0] >= 'A' && chu[0] < 'A' + colDapAn.Count) chiSoDung.Add(chu[0] - 'A');
                        else loi.Add($"đáp án đúng \"{chu}\" không hợp lệ");
                    }

                    for (var i = 0; i < dapAnText.Count; i++)
                    {
                        if (dapAnText[i] != "")
                        {
                            dapAns.Add(new DapAn { NoiDung = dapAnText[i], DapAnDung = chiSoDung.Contains(i) });
                        }
                        else if (chiSoDung.Contains(i))
                        {
                            loi.Add($"đáp án đúng {(char)('A' + i)} để trống nội dung");
                        }
                    }

                    if (dapAns.Count < 2) loi.Add("câu trắc nghiệm phải có ít nhất 2 đáp án");
                    else if (chuDung.Length == 0) loi.Add("thiếu cột đáp án đúng (vd: A hoặc A,C)");
                    else if (dapAns.Select(d => ChuanHoa(d.NoiDung)).Distinct().Count() != dapAns.Count)
                        loi.Add("các đáp án bị trùng nội dung");
                }

                if (loi.Count > 0)
                {
                    errors.AddRange(loi.Select(l => $"Dòng {r}: {l}"));
                    continue;
                }

                var khoa = ICauHoiService.TaoKhoa(danhMucId!.Value, noiDung);
                if (daCoTrongFile.TryGetValue(khoa, out var dongTruoc))
                {
                    errors.Add($"Dòng {r}: trùng câu hỏi ở dòng {dongTruoc} (cùng danh mục và nội dung)");
                    continue;
                }
                daCoTrongFile[khoa] = r;

                if (daCoTrongDb.Contains(khoa))
                {
                    soTrung++; // đã có trong hệ thống -> bỏ qua
                    continue;
                }

                rows.Add(new CauHoiImportRow
                {
                    Dong = r, DanhMucCauHoiId = danhMucId.Value, NoiDung = noiDung, LoaiCauHoi = loai!.Value,
                    MucDo = mucDo!.Value, DiemMacDinh = diem, GiaiThich = O(colGt), DapAns = dapAns
                });
            }
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Không đọc được file Excel. Vui lòng kiểm tra lại định dạng file.");
            await LoadGiaoVienChoImport(giaoVienId);
            return View();
        }

        if (errors.Count > 0)
        {
            ViewBag.Errors = errors;
            await LoadGiaoVienChoImport(giaoVienId);
            return View();
        }

        if (rows.Count == 0)
        {
            ModelState.AddModelError(string.Empty, soTrung > 0
                ? $"Tất cả {soTrung} câu hỏi trong file đã có trong hệ thống, không có câu nào mới"
                : "File không có câu hỏi nào");
            await LoadGiaoVienChoImport(giaoVienId);
            return View();
        }

        var added = await _cauHoiService.ImportCauHoiAsync(rows, gvId.Value);
        TempData["success"] = $"Nhập Excel thành công: thêm mới {added} câu hỏi" +
                              (soTrung > 0 ? $", bỏ qua {soTrung} câu đã có" : "");
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> DownloadTemplate()
    {
        var nodes = await _danhMucCauHoiService.GetDanhMucTreeAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("CauHoi");
        string[] tieuDe =
        {
            "Mã môn học", "Danh mục", "Nội dung câu hỏi", "Loại câu hỏi", "Mức độ", "Điểm",
            "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D", "Đáp án đúng", "Giải thích"
        };
        for (var i = 0; i < tieuDe.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = tieuDe[i];
        }
        sheet.Column(1).Style.NumberFormat.Format = "@";

        // Dòng mẫu dùng danh mục đầu tiên có sẵn
        var mau = nodes.FirstOrDefault();
        var maMon = mau?.DanhMuc.MonHoc?.MaMonHoc ?? "";
        var dm = mau == null ? "" : string.Join(" > ", mau.ToTien.Select(t => t.TenDanhMuc).Append(mau.DanhMuc.TenDanhMuc));
        object[][] dong =
        {
            new object[] { maMon, dm, "Câu hỏi trắc nghiệm mẫu (1 đáp án đúng)?", "Trắc nghiệm", "Dễ", 1,
                           "Phương án 1", "Phương án 2", "Phương án 3", "Phương án 4", "B", "Giải thích (không bắt buộc)" },
            new object[] { maMon, dm, "Câu hỏi trắc nghiệm mẫu (nhiều đáp án đúng)?", "Trắc nghiệm", "Khó", 1.5,
                           "Phương án 1", "Phương án 2", "Phương án 3", "", "A,C", "" },
            new object[] { maMon, dm, "Câu hỏi tự luận mẫu: trình bày ...", "Tự luận", "Trung bình", 2,
                           "", "", "", "", "", "Gợi ý chấm điểm" }
        };
        for (var r = 0; r < dong.Length; r++)
        {
            for (var c = 0; c < dong[r].Length; c++)
            {
                sheet.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(dong[r][c]);
            }
        }

        // Dropdown chọn sẵn cho Loại câu hỏi và Mức độ
        sheet.Range("D2:D1000").CreateDataValidation().List("\"Trắc nghiệm,Tự luận\"");
        sheet.Range("E2:E1000").CreateDataValidation().List("\"Dễ,Trung bình,Khó,Rất khó\"");
        StyleHeader(sheet.Range(1, 1, 1, tieuDe.Length));
        sheet.Columns().AdjustToContents();
        sheet.Column(3).Width = 50;

        // Sheet tham khảo: danh mục hiện có (copy cột Mã môn học + Danh mục sang sheet CauHoi)
        var ref_ = workbook.Worksheets.Add("DanhSachDanhMuc");
        ref_.Cell(1, 1).Value = "Mã môn học";
        ref_.Cell(1, 2).Value = "Danh mục";
        ref_.Cell(1, 3).Value = "Tên môn học";
        ref_.Column(1).Style.NumberFormat.Format = "@";
        for (var i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            ref_.Cell(i + 2, 1).Value = n.DanhMuc.MonHoc?.MaMonHoc ?? "";
            ref_.Cell(i + 2, 2).Value = string.Join(" > ", n.ToTien.Select(t => t.TenDanhMuc).Append(n.DanhMuc.TenDanhMuc));
            ref_.Cell(i + 2, 3).Value = n.DanhMuc.MonHoc?.TenMonHoc ?? "";
        }
        StyleHeader(ref_.Range(1, 1, 1, 3));
        ref_.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapCauHoi.xlsx");
    }

    private static void StyleHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
    }

    // Chuẩn hóa chuỗi để so sánh: Unicode NFC, chữ thường, gộp khoảng trắng
    private static string ChuanHoa(string s) =>
        string.Join(' ', s.Normalize().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string KhoaDanhMuc(int monHocId, IEnumerable<string> duongDan) =>
        monHocId + "|" + string.Join(">", duongDan.Select(ChuanHoa));

    private static int? DocLoai(string s) => ChuanHoa(s) switch
    {
        "1" or "trắc nghiệm" or "tn" => 1,
        "2" or "tự luận" or "tl" => 2,
        _ => null
    };

    private static int? DocMucDo(string s) => ChuanHoa(s) switch
    {
        "1" or "dễ" => 1,
        "2" or "trung bình" or "tb" => 2,
        "3" or "khó" => 3,
        "4" or "rất khó" => 4,
        _ => null
    };

    // Tài khoản không phải giảng viên (Admin) phải chọn giảng viên cho các câu hỏi được nhập
    private async Task LoadGiaoVienChoImport(int? giaoVienId)
    {
        var userId = _userManager.GetUserId(User);
        var giaoVien = userId == null ? null : await _cauHoiService.GetGiaoVienByUserIdAsync(userId);
        ViewData["GiaoVienHienTai"] = giaoVien;
        if (giaoVien == null)
        {
            var giaoViens = await _cauHoiService.GetAllGiaoVienAsync();
            ViewData["GiaoVienList"] = new SelectList(
                giaoViens.Select(g => new { g.Id, Ten = $"{g.HoTen} ({g.MaGiaoVien})" }), "Id", "Ten", giaoVienId);
        }
    }

    #endregion

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

    private void ValidateHinhAnh(CauHoiFormVM vm)
    {
        if (vm.HinhAnhFile == null)
        {
            return;
        }
        var loi = HinhAnhCauHoiStorage.KiemTra(vm.HinhAnhFile);
        if (loi != null)
        {
            ModelState.AddModelError(nameof(vm.HinhAnhFile), loi);
        }
    }

    private static CauHoi TaoCauHoiTuForm(CauHoiFormVM vm) => new()
    {
        DanhMucCauHoiId = vm.DanhMucCauHoiId,
        NoiDung = vm.NoiDung.Trim(),
        LoaiCauHoi = vm.LoaiCauHoi,
        MucDo = vm.MucDo,
        DiemMacDinh = vm.DiemMacDinh,
        GiaiThich = vm.GiaiThich?.Trim() ?? string.Empty
    };

    // Admin sửa được mọi câu hỏi; giảng viên chỉ sửa được câu hỏi do mình tạo
    private async Task<bool> CoQuyenSuaAsync(CauHoi cauHoi)
    {
        if (User.IsInRole(SD.Role_Admin))
        {
            return true;
        }
        var userId = _userManager.GetUserId(User);
        var giaoVien = userId == null ? null : await _cauHoiService.GetGiaoVienByUserIdAsync(userId);
        return giaoVien != null && giaoVien.Id == cauHoi.GiaoVienId;
    }

    private async Task<DanhMucCauHoi?> ValidateDanhMucAsync(CauHoiFormVM vm)
    {
        var danhMuc = vm.DanhMucCauHoiId > 0
            ? await _danhMucCauHoiService.GetDanhMucCauHoiByIdAsync(vm.DanhMucCauHoiId)
            : null;
        if (vm.DanhMucCauHoiId > 0 && danhMuc == null)
        {
            ModelState.AddModelError(nameof(vm.DanhMucCauHoiId), "Danh mục không tồn tại");
        }
        return danhMuc;
    }

    // Kiểm tra đáp án và trả về danh sách sẽ lưu (bỏ các dòng để trống). Câu tự luận => không có đáp án.
    private List<DapAn> ValidateDapAns(CauHoiFormVM vm)
    {
        if (vm.LoaiCauHoi != 1)
        {
            return new List<DapAn>();
        }

        if (vm.DapAns.Any(d => string.IsNullOrWhiteSpace(d.NoiDung) && d.DapAnDung))
        {
            ModelState.AddModelError(nameof(vm.DapAns), "Đáp án được đánh dấu đúng phải có nội dung");
        }

        var dapAns = vm.DapAns
            .Where(d => !string.IsNullOrWhiteSpace(d.NoiDung))
            .Select(d => new DapAn { Id = d.Id, NoiDung = d.NoiDung!.Trim(), DapAnDung = d.DapAnDung })
            .ToList();

        if (dapAns.Count < 2)
        {
            ModelState.AddModelError(nameof(vm.DapAns), "Câu trắc nghiệm phải có ít nhất 2 đáp án");
        }
        else if (!dapAns.Any(d => d.DapAnDung))
        {
            ModelState.AddModelError(nameof(vm.DapAns), "Vui lòng đánh dấu ít nhất 1 đáp án đúng");
        }
        else if (dapAns.Select(d => d.NoiDung.ToLowerInvariant()).Distinct().Count() != dapAns.Count)
        {
            ModelState.AddModelError(nameof(vm.DapAns), "Các đáp án không được trùng nội dung");
        }
        return dapAns;
    }

    // Trả về Id giảng viên sẽ gắn với câu hỏi; null (kèm lỗi trong ModelState) nếu không xác định được
    private async Task<int?> XacDinhGiaoVienAsync(int? giaoVienIdChon)
    {
        var userId = _userManager.GetUserId(User);
        var giaoVien = userId == null ? null : await _cauHoiService.GetGiaoVienByUserIdAsync(userId);
        if (giaoVien != null)
        {
            return giaoVien.Id;
        }

        if (giaoVienIdChon == null || giaoVienIdChon <= 0)
        {
            ModelState.AddModelError(nameof(CauHoiFormVM.GiaoVienId), "Vui lòng chọn giảng viên");
            return null;
        }
        if (!await _cauHoiService.GiaoVienExistsAsync(giaoVienIdChon.Value))
        {
            ModelState.AddModelError(nameof(CauHoiFormVM.GiaoVienId), "Giảng viên không tồn tại");
            return null;
        }
        return giaoVienIdChon.Value;
    }

    // Chuẩn bị dữ liệu để hiển thị (lại) form Thêm/Sửa
    private async Task ChuanBiHienLaiForm(CauHoiFormVM vm, DanhMucCauHoi? danhMuc)
    {
        if (vm.LoaiCauHoi == 1)
        {
            while (vm.DapAns.Count < 2)
            {
                vm.DapAns.Add(new DapAnInputVM());
            }
        }
        vm.MonHocId ??= danhMuc?.MonHocId;
        if (vm.Id > 0)
        {
            vm.SoDeThiDangDung = await _cauHoiService.CountDeThiDangDungAsync(vm.Id);
        }
        await LoadDropdowns(vm);
    }

    // Đổ dữ liệu cho dropdown Môn học, Danh mục (dạng cây) và Giảng viên
    private async Task LoadDropdowns(CauHoiFormVM vm)
    {
        var monHocs = await _danhMucCauHoiService.GetAllMonHocAsync();
        ViewData["MonHocList"] = new SelectList(monHocs, nameof(MonHoc.Id), nameof(MonHoc.TenMonHoc), vm.MonHocId);
        ViewData["DanhMucList"] = await _danhMucCauHoiService.GetDanhMucTreeAsync();

        // Khi sửa: giảng viên giữ nguyên, form chỉ hiển thị tên => không cần danh sách
        if (vm.Id > 0)
        {
            return;
        }

        // Tài khoản đang đăng nhập là giảng viên => không cần chọn giảng viên
        var userId = _userManager.GetUserId(User);
        var giaoVien = userId == null ? null : await _cauHoiService.GetGiaoVienByUserIdAsync(userId);
        ViewData["GiaoVienHienTai"] = giaoVien;
        if (giaoVien == null)
        {
            var giaoViens = await _cauHoiService.GetAllGiaoVienAsync();
            ViewData["GiaoVienList"] = new SelectList(
                giaoViens.Select(g => new { g.Id, Ten = $"{g.HoTen} ({g.MaGiaoVien})" }), "Id", "Ten", vm.GiaoVienId);
        }
    }

    #region  CALL API

    public async Task<IActionResult> GetAllCauHoi()
    {
        var cauHois = await _cauHoiService.GetAllCauHoiAsync();

        // Đường dẫn đầy đủ của danh mục (danh mục nhiều cấp), vd: "Ngôn ngữ SQL › Truy vấn nâng cao"
        var cay = (await _danhMucCauHoiService.GetDanhMucTreeAsync()).ToDictionary(n => n.DanhMuc.Id);

        var data = cauHois.Select(c => new
        {
            c.Id,
            c.NoiDung,
            DanhMuc = cay.TryGetValue(c.DanhMucCauHoiId, out var n) ? n.DuongDanDayDu : c.TenDanhMuc,
            DanhMucLoc = cay.TryGetValue(c.DanhMucCauHoiId, out var n2) ? n2.KhoaLoc : "",
            c.DanhMucCauHoiId,
            c.TenMonHoc,
            c.LoaiCauHoi,
            TenLoaiCauHoi = CauHoiLabel.TenLoaiCauHoi(c.LoaiCauHoi),
            c.MucDo,
            TenMucDo = CauHoiLabel.TenMucDo(c.MucDo),
            c.DiemMacDinh,
            c.TenGiaoVien,
            c.SoDapAn,
            c.CoHinhAnh
        });
        return Json(new { data });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ cauhoi.js
    public async Task<IActionResult> DeleteCauHoi(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        var cauHoi = await _cauHoiService.GetCauHoiByIdAsync(id.Value);
        if (cauHoi == null)
        {
            return Json(new { success = false, message = "Không tìm thấy câu hỏi cần xóa" });
        }

        if (!await CoQuyenSuaAsync(cauHoi))
        {
            return Json(new { success = false, message = "Bạn chỉ được xóa câu hỏi do mình tạo" });
        }

        // Không cho xóa câu hỏi đang nằm trong đề thi (xóa sẽ làm đề thi mất câu hỏi)
        var soDeThi = await _cauHoiService.CountDeThiDangDungAsync(id.Value);
        if (soDeThi > 0)
        {
            return Json(new { success = false, message = $"Câu hỏi đang được dùng trong {soDeThi} đề thi, không thể xóa" });
        }

        await _cauHoiService.DeleteCauHoiAsync(id.Value);
        _hinhAnh.Xoa(cauHoi.HinhAnh);
        return Json(new { success = true, message = "Xóa câu hỏi thành công" });
    }

    #endregion
}
